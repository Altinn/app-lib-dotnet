package config

import (
	"os"
	"reflect"
	"testing"

	"github.com/go-playground/validator/v10"
	. "github.com/onsi/gomega"
)

func TestConfig_Missing_Values_Fail(t *testing.T) {
	RegisterTestingT(t)

	file, err := os.CreateTemp(os.TempDir(), "*.env")
	Expect(err).NotTo(HaveOccurred())
	defer file.Close()
	defer os.Remove(file.Name())

	_, err = file.WriteString("maskinporten_api.url=https://example.com")
	Expect(err).NotTo(HaveOccurred())

	cfg, err := LoadConfig(file.Name())
	Expect(cfg).To(BeNil())
	Expect(err).To(HaveOccurred())
	_, ok := err.(validator.ValidationErrors)
	errType := reflect.TypeOf(err)
	Expect(errType.String()).To(Equal("validator.ValidationErrors"))
	Expect(ok).To(BeTrue())
}

func TestConfig_TestEnv_Loads_Ok(t *testing.T) {
	RegisterTestingT(t)

	cfg, err := LoadConfig("")
	Expect(err).NotTo(HaveOccurred())
	Expect(cfg).NotTo(BeNil())
	Expect(cfg.MaskinportenApi.ClientId).To(Equal("64d8055d-bf0c-4ee2-979e-d2bbe996a9f5"))
	Expect(cfg.MaskinportenApi.Url).To(Equal("https://maskinporten.dev"))
	Expect(cfg.MaskinportenApi.Jwk).NotTo(BeNil())
}
