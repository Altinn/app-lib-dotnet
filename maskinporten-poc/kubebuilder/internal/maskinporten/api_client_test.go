package maskinporten

import (
	"net/http"
	"net/http/httptest"
	"testing"

	"altinn.operator/maskinporten/internal/config"
	. "github.com/onsi/gomega"
)

func TestWellKnownConfig(t *testing.T) {
	RegisterTestingT(t)

	cfg, err := config.LoadConfig("")
	Expect(err).NotTo(HaveOccurred())
	Expect(cfg).NotTo(BeNil())

	apiClient, err := NewApiClient(&cfg.MaskinportenApi)
	Expect(err).NotTo(HaveOccurred())

	config1, err := apiClient.GetWellKnownConfiguration()
	Expect(err).NotTo(HaveOccurred())
	Expect(config1).NotTo(BeNil())

	config2, err := apiClient.GetWellKnownConfiguration()
	Expect(err).NotTo(HaveOccurred())
	Expect(config2).NotTo(BeNil())
	config3 := *config1
	Expect(config1 == config2).To(BeTrue()) // Due to cache
	Expect(config1 == &config3).To(BeFalse())
}

func TestCreateGrant(t *testing.T) {
	RegisterTestingT(t)

	cfg, err := config.LoadConfig("")
	Expect(err).NotTo(HaveOccurred())
	Expect(cfg).NotTo(BeNil())

	client, err := NewApiClient(&cfg.MaskinportenApi)
	Expect(err).NotTo(HaveOccurred())

	concreteClient, ok := client.(*apiClient)
	Expect(ok).To(BeTrue())

	grant, err := concreteClient.createGrant()
	Expect(err).NotTo(HaveOccurred())
	Expect(grant).NotTo(BeNil())
}

// func TestFetchAccessToken(t *testing.T) {
// 	RegisterTestingT(t)

// 	cfg, err := config.LoadConfig("")
// 	Expect(err).NotTo(HaveOccurred())
// 	Expect(cfg).NotTo(BeNil())

// 	client, err := NewApiClient(&cfg.MaskinportenApi)
// 	Expect(err).NotTo(HaveOccurred())

// 	concreteClient, ok := client.(*apiClient)
// 	Expect(ok).To(BeTrue())

// 	token, err := concreteClient.accessTokenFetcher()
// 	Expect(err).NotTo(HaveOccurred())
// 	Expect(token.AccessToken).NotTo(BeNil())
// }

func TestFetchAccessTokenWithHTTPTest(t *testing.T) {
	g := NewWithT(t) // Initialize a new GomegaWithT instance for this test

	// Create a mock server
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusOK)
		w.Write([]byte(`{"access_token":"mock_access_token","token_type":"Bearer","expires_in":3600}`))
	}))
	defer server.Close()

	cfg, err := config.LoadConfig("")
	g.Expect(err).NotTo(HaveOccurred())
	g.Expect(cfg).NotTo(BeNil())

	cfg.MaskinportenApi.Url = server.URL

	client, err := NewApiClient(&cfg.MaskinportenApi)
	g.Expect(err).NotTo(HaveOccurred())

	concreteClient, ok := client.(*apiClient)
	g.Expect(ok).To(BeTrue())

	token, err := concreteClient.accessTokenFetcher()
	g.Expect(err).NotTo(HaveOccurred())
	g.Expect(token.AccessToken).NotTo(BeNil())
}
