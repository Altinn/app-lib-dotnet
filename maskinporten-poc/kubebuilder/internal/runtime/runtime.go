package runtime

import (
	"altinn.operator/maskinporten/internal/config"
	"altinn.operator/maskinporten/internal/maskinporten/api"
)

type Runtime interface {
	GetConfig() *config.Config
	GetMaskinportenClient() api.ApiClient
}
