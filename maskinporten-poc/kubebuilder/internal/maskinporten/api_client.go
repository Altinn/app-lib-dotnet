package maskinporten

import (
	"context"
	"encoding/json"
	"fmt"
	"net/http"
	"net/url"
	"sync"
	"time"

	"altinn.operator/maskinporten/internal/config"
	"altinn.operator/maskinporten/internal/maskinporten/api"
	"github.com/cenkalti/backoff/v4"
	"github.com/go-jose/go-jose/v4"
	"github.com/go-jose/go-jose/v4/jwt"
	"github.com/google/uuid"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/trace"
)

type apiClient struct {
	config      *config.MaskinportenApiConfig
	client      http.Client
	jwk         jose.JSONWebKey
	wellKnown   Cached[api.WellKnownResponse]
	accessToken Cached[tokenResponse]
	tracer      trace.Tracer
}

// Docs:
// - https://docs.digdir.no/docs/Maskinporten/maskinporten_guide_apikonsument
// - https://docs.digdir.no/docs/Maskinporten/maskinporten_protocol_token
// - https://docs.digdir.no/docs/Maskinporten/maskinporten_func_wellknown

var _ api.ApiClient = (*apiClient)(nil)

func NewApiClient(config *config.MaskinportenApiConfig) (api.ApiClient, error) {
	jwk := jose.JSONWebKey{}
	if err := json.Unmarshal([]byte(config.Jwk), &jwk); err != nil {
		return nil, err
	}

	client := &apiClient{
		config: config,
		client: http.Client{Transport: otelhttp.NewTransport(http.DefaultTransport)},
		jwk:    jwk,
		tracer: otel.Tracer("maskinporten.ApiClient"),
	}

	client.wellKnown = NewCached(5*time.Minute, client.wellKnownFetcher)
	client.accessToken = NewCached(1*time.Minute, client.accessTokenFetcher)

	return client, nil
}

func (c *apiClient) createReq(ctx context.Context, endpoint string) (*http.Request, error) {
	// Fetch the access token from the cache.
	tokenResponse, err := c.accessToken.Get(ctx)
	if err != nil {
		return nil, fmt.Errorf("failed to get access token: %w", err)
	}

	// Prepare the request.
	req, err := http.NewRequestWithContext(ctx, "POST", endpoint, nil)
	if err != nil {
		return nil, fmt.Errorf("failed to create new request: %w", err)
	}

	// Set necessary headers.
	req.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	req.Header.Set("Authorization", "Bearer "+tokenResponse.AccessToken)

	return req, nil
}

func (c *apiClient) GetWellKnownConfiguration(ctx context.Context) (*api.WellKnownResponse, error) {
	ctx, span := c.tracer.Start(ctx, "GetWellKnownConfiguration")
	defer span.End()

	return c.wellKnown.Get(ctx)
}

func (c *apiClient) createGrant(ctx context.Context) (*string, error) {
	wellKnown, err := c.wellKnown.Get(ctx)
	if err != nil {
		return nil, err
	}

	exp := time.Now().Add(60 * time.Second)
	issuedAt := time.Now()

	pubClaims := jwt.Claims{
		Audience:  []string{wellKnown.Issuer},
		Issuer:    c.config.ClientId,
		IssuedAt:  jwt.NewNumericDate(issuedAt),
		NotBefore: jwt.NewNumericDate(issuedAt),
		Expiry:    jwt.NewNumericDate(exp),
		ID:        uuid.New().String(),
	}

	privClaims := struct {
		Scope string `json:"scope"`
	}{
		Scope: c.config.Scope,
	}

	signer, err := jose.NewSigner(jose.SigningKey{Algorithm: jose.RS256, Key: c.jwk}, (&jose.SignerOptions{}).WithType("JWT"))
	if err != nil {
		return nil, err
	}

	signedToken, err := jwt.Signed(signer).Claims(pubClaims).Claims(privClaims).Serialize()
	if err != nil {
		return nil, err
	}

	return &signedToken, nil
}

func (c *apiClient) accessTokenFetcher(ctx context.Context) (*tokenResponse, error) {
	grant, err := c.createGrant(ctx)
	if err != nil {
		return nil, err
	}

	endpoint, err := url.JoinPath(c.config.Url, "/token")
	if err != nil {
		return nil, err
	}

	urlEncodedContent := url.Values{
		"grant_type": {"urn:ietf:params:oauth:grant-type:jwt-bearer"},
		"assertion":  {*grant},
	}

	endpoint += "?" + urlEncodedContent.Encode()

	req, err := http.NewRequestWithContext(ctx, "POST", endpoint, nil)
	if err != nil {
		return nil, err
	}

	req.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	resp, err := c.RetryableHTTPDo(req)
	if err != nil {
		return nil, err
	}

	if resp.StatusCode != 200 {
		resp.Body.Close()
		return nil, fmt.Errorf("unexpected status code: %d", resp.StatusCode)
	}

	tokenResp, err := deserialize[tokenResponse](resp)
	if err != nil {
		return nil, err
	}

	return &tokenResp, nil
}

type tokenResponse struct {
	AccessToken string `json:"access_token"`
	TokenType   string `json:"token_type"`
	ExpiresIn   int    `json:"expires_in"`
	Scope       string `json:"scope"`
}

func (c *apiClient) wellKnownFetcher(ctx context.Context) (*api.WellKnownResponse, error) {
	endpoint, err := url.JoinPath(c.config.Url, "/.well-known/oauth-authorization-server")
	if err != nil {
		return nil, err
	}

	req, err := http.NewRequestWithContext(ctx, "GET", endpoint, nil)
	if err != nil {
		return nil, err
	}

	resp, err := c.client.Do(req)
	if err != nil {
		return nil, err
	}

	if resp.StatusCode != 200 {
		resp.Body.Close()
		return nil, fmt.Errorf("unexpected status code: %d", resp.StatusCode)
	}

	wellKnownResp, err := deserialize[api.WellKnownResponse](resp)
	if err != nil {
		return nil, err
	}
	return &wellKnownResp, nil
}

func deserialize[T any](resp *http.Response) (T, error) {
	defer resp.Body.Close()

	var result T
	err := json.NewDecoder(resp.Body).Decode(&result)
	if err != nil {
		return result, err
	}

	return result, err
}

type Cached[T any] struct {
	mutex            sync.RWMutex
	retriever        func(ctx context.Context) (*T, error)
	current          *T
	currentFetchedAt time.Time
	expireAfter      time.Duration
}

func NewCached[T any](expireAfter time.Duration, retriever func(ctx context.Context) (*T, error)) Cached[T] {
	return Cached[T]{
		retriever:   retriever,
		mutex:       sync.RWMutex{},
		expireAfter: expireAfter,
	}
}

func (c *Cached[T]) Get(ctx context.Context) (*T, error) {
	c.mutex.RLock()
	now := time.Now()
	if c.currentFetchedAt.IsZero() || now.Sub(c.currentFetchedAt) > c.expireAfter {
		c.mutex.RUnlock()
		c.mutex.Lock()
		defer c.mutex.Unlock()

		now = time.Now()
		if now.Sub(c.currentFetchedAt) <= c.expireAfter {
			return c.current, nil
		}

		value, err := c.retriever(ctx)
		if err != nil {
			return nil, err
		}

		c.current = value
		c.currentFetchedAt = now
		return c.current, nil
	}
	defer c.mutex.RUnlock()

	return c.current, nil
}

// RetryableHTTPDo performs an HTTP request with retry logic.
func (c *apiClient) RetryableHTTPDo(req *http.Request) (*http.Response, error) {
	var resp *http.Response
	var err error

	operation := func() error {
		resp, err = c.client.Do(req)
		if err != nil {
			return err // Network error, retry.
		}
		if resp.StatusCode >= 500 { // Retrying on 5xx server errors.
			return fmt.Errorf("server error: %v", resp.Status)
		}
		return nil // No retry needed - success or client side error
	}

	backoffStrategy := backoff.NewExponentialBackOff()
	// Default setting is to 1.5x the time interval for every failure
	backoffStrategy.InitialInterval = 1 * time.Second
	backoffStrategy.MaxInterval = 30 * time.Second
	backoffStrategy.MaxElapsedTime = 2 * time.Minute

	err = backoff.Retry(operation, backoffStrategy)
	if err != nil {
		return nil, err
	}

	return resp, nil
}
