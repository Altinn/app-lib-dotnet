package maskinporten

import (
	"fmt"
	"sync"

	"altinn.operator/maskinporten/internal/maskinporten/api"
	"github.com/google/uuid"
)

type clientManager struct {
	mutex   sync.Mutex
	clients map[string]*api.ClientInfo
}

var _ api.ClientManager = (*clientManager)(nil)

func NewClientManager() api.ClientManager {
	return &clientManager{
		mutex:   sync.Mutex{},
		clients: make(map[string]*api.ClientInfo),
	}
}

func (s *clientManager) Get(appId string) (*api.ClientInfo, error) {
	s.mutex.Lock()
	defer s.mutex.Unlock()

	client, ok := s.clients[appId]
	if !ok {
		// TODO: fetch client from API
		return nil, nil
	}

	return client, nil
}

func (s *clientManager) Reconcile(info *api.ClientInfo) (*api.ClientInfo, bool, error) {
	s.mutex.Lock()
	defer s.mutex.Unlock()

	// TODO: sync with client API

	client, ok := s.clients[info.AppId]
	if !ok {
		uuid, err := uuid.NewRandom()
		if err != nil {
			return nil, false, fmt.Errorf("failed to generate client ID: %w", err)
		}
		client = &api.ClientInfo{
			Id:     uuid.String(),
			AppId:  info.AppId,
			Scopes: info.Scopes,
		}
		s.clients[client.AppId] = client
		return client, true, nil
	} else {
		client.Scopes = info.Scopes
		// nothing to update yet
		return client, false, nil
	}
}

func (s *clientManager) Delete(appId string) error {
	s.mutex.Lock()
	defer s.mutex.Unlock()

	if _, ok := s.clients[appId]; !ok {
		return fmt.Errorf("client not found")
	}

	delete(s.clients, appId)
	// TODO: sync with client API
	return nil
}
