package controller

import (
	"altinn.operator/maskinporten/internal/maskinporten/api"
	corev1 "k8s.io/api/core/v1"
)

type maskinportenResourceKind int

const (
	ApiClientKind maskinportenResourceKind = iota + 1
	SecretKind
)

type maskinportenResource interface {
	kind() maskinportenResourceKind
}

type maskinportenResourceList []maskinportenResource

type maskinportenSecretResource struct {
	secret *corev1.Secret
}

func (r *maskinportenSecretResource) kind() maskinportenResourceKind {
	return SecretKind
}

type maskinportenApiClientResource struct {
	info *api.ClientInfo
}

func (r *maskinportenApiClientResource) kind() maskinportenResourceKind {
	return ApiClientKind
}

type reconciliationActionKind int

const (
	ActionUpsertKind reconciliationActionKind = iota + 1
	ActionDeleteKind
)

type reconciliationAction struct {
	kind     reconciliationActionKind
	resource maskinportenResource
}

type reconciliationActionList []*reconciliationAction
