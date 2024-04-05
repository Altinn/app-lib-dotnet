package runtime

import (
	"altinn.operator/maskinporten/internal/config"
	internalContext "altinn.operator/maskinporten/internal/context"
	"altinn.operator/maskinporten/internal/maskinporten/api"
)

type Runtime interface {
	GetConfig() *config.Config
	GetMaskinportenApiClient() api.ApiClient
	GetMaskinportenClientManager() api.ClientManager
	GetOperatorContext() *internalContext.OperatorContext
}
