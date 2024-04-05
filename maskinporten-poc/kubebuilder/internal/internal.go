package internal

import (
	"altinn.operator/maskinporten/internal/config"
	internalContext "altinn.operator/maskinporten/internal/context"
	"altinn.operator/maskinporten/internal/maskinporten"
	"altinn.operator/maskinporten/internal/maskinporten/api"
	rt "altinn.operator/maskinporten/internal/runtime"
)

type runtime struct {
	config             config.Config
	maskinportenClient api.ApiClient
	operatorContext    internalContext.OperatorContext
	clientManager      api.ClientManager
}

var _ rt.Runtime = (*runtime)(nil)

func NewRuntime(envFile string) (rt.Runtime, error) {
	cfg, err := config.LoadConfig(envFile)
	if err != nil {
		return nil, err
	}

	maskinportenClient, err := maskinporten.NewApiClient(&cfg.MaskinportenApi)
	if err != nil {
		return nil, err
	}

	operatorContext, err := internalContext.Discover()
	if err != nil {
		return nil, err
	}

	rt := &runtime{
		config:             *cfg,
		maskinportenClient: maskinportenClient,
		operatorContext:    *operatorContext,
		clientManager:      maskinporten.NewClientManager(),
	}

	return rt, nil
}

func (r *runtime) GetConfig() *config.Config {
	return &r.config
}

func (r *runtime) GetMaskinportenApiClient() api.ApiClient {
	return r.maskinportenClient
}

func (r *runtime) GetMaskinportenClientManager() api.ClientManager {
	return r.clientManager
}

func (r *runtime) GetOperatorContext() *internalContext.OperatorContext {
	return &r.operatorContext
}
