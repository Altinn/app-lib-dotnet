package maskinporten

import (
	"testing"

	"altinn.operator/maskinporten/internal/config"
	. "github.com/onsi/gomega"
)

func TestWellKnownConfig(t *testing.T) {
	RegisterTestingT(t)

	cfg, err := config.LoadConfig("../test.env")
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

	cfg, err := config.LoadConfig("../test.env")
	Expect(err).NotTo(HaveOccurred())
	Expect(cfg).NotTo(BeNil())

	apiClient, err := NewApiClient(&cfg.MaskinportenApi)
	Expect(err).NotTo(HaveOccurred())

	grant, err := apiClient.createGrant()
	Expect(err).NotTo(HaveOccurred())
	Expect(grant).NotTo(BeNil())
}

// func TestFetchAccessToken(t *testing.T) {
// 	RegisterTestingT(t)

// 	cfg, err := config.LoadConfig("../test.env")
// 	Expect(err).NotTo(HaveOccurred())
// 	Expect(cfg).NotTo(BeNil())

// 	apiClient, err := NewApiClient(&cfg.MaskinportenApi)
// 	Expect(err).NotTo(HaveOccurred())

// 	token, err := apiClient.accessTokenFetcher()
// 	Expect(err).NotTo(HaveOccurred())
// 	Expect(token.AccessToken).NotTo(BeNil())
// }
