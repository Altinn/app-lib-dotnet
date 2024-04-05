package config

import (
	"fmt"
	"os"
	"path"

	"github.com/knadh/koanf/parsers/dotenv"
	"github.com/knadh/koanf/providers/file"
	"github.com/knadh/koanf/v2"

	"github.com/go-playground/validator/v10"
)

type Config struct {
	MaskinportenApi MaskinportenApiConfig `koanf:"maskinporten_api" validate:"required"`
}

type MaskinportenApiConfig struct {
	ClientId string `koanf:"client_id" validate:"required"`
	Url      string `koanf:"url" validate:"required,http_url"`
	Jwk      string `koanf:"jwk" validate:"required,json"`
	Scope    string `koanf:"scope" validate:"required"`
}

var (
	k      = koanf.New(".")
	parser = dotenv.ParserEnv("", ".", func(s string) string { return s })
)

func tryFindProjectRoot() {
	for {
		if _, err := os.Stat("go.mod"); err == nil {
			return
		}

		if err := os.Chdir(".."); err != nil {
			return
		}
	}
}

func LoadConfig(filePath string) (*Config, error) {
	tryFindProjectRoot()

	if filePath == "" {
		filePath = "local.env"
	}

	currentDir, err := os.Getwd()
	if err != nil {
		return nil, err
	}
	if _, err := os.Stat(filePath); os.IsNotExist(err) {
		if path.IsAbs(filePath) {
			return nil, fmt.Errorf("env file does not exist: '%s'", filePath)
		} else {
			return nil, fmt.Errorf("env file does not exist in '%s': '%s'", currentDir, filePath)
		}
	}

	if !path.IsAbs(filePath) {
		filePath = path.Join(currentDir, filePath)
	}

	if err := k.Load(file.Provider(filePath), parser); err != nil {
		return nil, fmt.Errorf("error loading config '%s': %w", filePath, err)
	}

	var cfg Config

	if err := k.Unmarshal("", &cfg); err != nil {
		return nil, fmt.Errorf("error unmarshalling config '%s': %w", filePath, err)
	}

	validate := validator.New(validator.WithRequiredStructEnabled())

	if err := validate.Struct(cfg); err != nil {
		return nil, err
	}

	// k.Print() // Uncomment to print the config, only for debug, there be secrets

	return &cfg, nil
}
