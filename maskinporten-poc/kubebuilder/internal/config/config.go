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

func LoadConfig(filePath string) (*Config, error) {
	if filePath == "" {
		filePath = ".env"
	}

	if !path.IsAbs(filePath) {
		currentDir, err := os.Getwd()
		if err != nil {
			return nil, err
		}
		filePath = path.Join(currentDir, filePath)
	}

	_, err := os.Stat(filePath)
	if os.IsNotExist(err) {
		return nil, fmt.Errorf("env file does not exist: '%s'", filePath)
	} else if err != nil {
		return nil, err
	}

	if err := k.Load(file.Provider(filePath), parser); err != nil {
		return nil, err
	}

	var cfg Config

	if err := k.Unmarshal("", &cfg); err != nil {
		return nil, err
	}

	validate := validator.New(validator.WithRequiredStructEnabled())

	if err := validate.Struct(cfg); err != nil {
		return nil, err
	}

	k.Print()

	return &cfg, nil
}
