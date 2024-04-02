package maskinporten

import "net/http"

type ApiClient struct {
	client *http.Client
}

func NewApiClient() *ApiClient {
	return &ApiClient{
		client: &http.Client{},
	}
}
