package maskinporten

import (
	"encoding/json"
	"fmt"
	"net/http"
	"net/url"
	"sync"
	"time"

	"altinn.operator/maskinporten/internal/config"
	"altinn.operator/maskinporten/internal/maskinporten/api"
	"github.com/go-jose/go-jose/v4"
	"github.com/go-jose/go-jose/v4/jwt"
	"github.com/google/uuid"
)

type apiClient struct {
	config      *config.MaskinportenApiConfig
	client      http.Client
	jwk         jose.JSONWebKey
	wellKnown   Cached[api.WellKnownResponse]
	accessToken Cached[tokenResponse]
}

// Docs:
// - https://docs.digdir.no/docs/Maskinporten/maskinporten_guide_apikonsument
// - https://docs.digdir.no/docs/Maskinporten/maskinporten_protocol_token
// - https://docs.digdir.no/docs/Maskinporten/maskinporten_func_wellknown

func NewApiClient(config *config.MaskinportenApiConfig) (*apiClient, error) {
	jwk := jose.JSONWebKey{}
	if err := json.Unmarshal([]byte(config.Jwk), &jwk); err != nil {
		return nil, err
	}

	client := &apiClient{
		config: config,
		client: http.Client{},
		jwk:    jwk,
	}

	client.wellKnown = NewCached(5*time.Minute, client.wellKnownFetcher)
	client.accessToken = NewCached(1*time.Minute, client.accessTokenFetcher)

	return client, nil
}

func (c *apiClient) GetWellKnownConfiguration() (*api.WellKnownResponse, error) {
	return c.wellKnown.Get()
}

func (c *apiClient) createGrant() (*string, error) {
	wellKnown, err := c.wellKnown.Get()
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

func (c *apiClient) accessTokenFetcher() (*tokenResponse, error) {
	grant, err := c.createGrant()
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

	req, err := http.NewRequest("POST", endpoint, nil)
	if err != nil {
		return nil, err
	}

	req.Header.Set("Content-Type", "application/x-www-form-urlencoded")
	resp, err := c.client.Do(req)
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

func (c *apiClient) wellKnownFetcher() (*api.WellKnownResponse, error) {
	endpoint, err := url.JoinPath(c.config.Url, "/.well-known/oauth-authorization-server")
	if err != nil {
		return nil, err
	}

	req, err := http.NewRequest("GET", endpoint, nil)
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
	retriever        func() (*T, error)
	current          *T
	currentFetchedAt time.Time
	expireAfter      time.Duration
}

func NewCached[T any](expireAfter time.Duration, retriever func() (*T, error)) Cached[T] {
	return Cached[T]{
		retriever:   retriever,
		mutex:       sync.RWMutex{},
		expireAfter: expireAfter,
	}
}

func (c *Cached[T]) Get() (*T, error) {
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

		value, err := c.retriever()
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
