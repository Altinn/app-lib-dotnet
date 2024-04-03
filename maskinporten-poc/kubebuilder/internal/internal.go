package internal

import (
	"altinn.operator/maskinporten/internal/config"
	"altinn.operator/maskinporten/internal/maskinporten"
	"altinn.operator/maskinporten/internal/maskinporten/api"
	rt "altinn.operator/maskinporten/internal/runtime"
)

type runtime struct {
	config             config.Config
	maskinportenClient api.ApiClient
}

func NewRuntime(envFile string) (rt.Runtime, error) {
	cfg, err := config.LoadConfig(envFile)
	if err != nil {
		return nil, err
	}

	maskinportenClient, err := maskinporten.NewApiClient(&cfg.MaskinportenApi)
	if err != nil {
		return nil, err
	}

	rt := &runtime{
		config:             *cfg,
		maskinportenClient: maskinportenClient,
	}

	return rt, nil
}

func (r *runtime) GetConfig() *config.Config {
	return &r.config
}

func (r *runtime) GetMaskinportenClient() api.ApiClient {
	return r.maskinportenClient
}
