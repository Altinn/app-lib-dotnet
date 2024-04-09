package controller

import (
	"fmt"

	"altinn.operator/maskinporten/internal/maskinporten/api"
	corev1 "k8s.io/api/core/v1"
)

type maskinportenResourceKind int

const (
	ApiClientKind maskinportenResourceKind = iota + 1
	SecretKind
)

var resourceKindToString = map[maskinportenResourceKind]string{
	ApiClientKind: "ApiClient",
	SecretKind:    "Secret",
}

func (k maskinportenResourceKind) String() string {
	if s, ok := resourceKindToString[k]; ok {
		return s
	}
	return "Unknown"
}

type maskinportenResource interface {
	fmt.Stringer
	kind() maskinportenResourceKind
}

type maskinportenResourceList []maskinportenResource

type maskinportenSecretResource struct {
	secret *corev1.Secret
}

func (r *maskinportenSecretResource) kind() maskinportenResourceKind {
	return SecretKind
}

func (r *maskinportenSecretResource) String() string {
	return fmt.Sprintf("Secret/%s", r.secret.Name)
}

type maskinportenApiClientResource struct {
	info *api.ClientInfo
}

func (r *maskinportenApiClientResource) kind() maskinportenResourceKind {
	return ApiClientKind
}

func (r *maskinportenApiClientResource) String() string {
	return fmt.Sprintf("ApiClient/%s", r.info.AppId)
}

type reconciliationActionKind int

const (
	ActionUpsertKind reconciliationActionKind = iota + 1
	ActionDeleteKind
)

var actionKindToString = map[reconciliationActionKind]string{
	ActionUpsertKind: "Upsert",
	ActionDeleteKind: "Delete",
}

func (k reconciliationActionKind) String() string {
	if s, ok := actionKindToString[k]; ok {
		return s
	}
	return "Unknown"
}

type reconciliationAction struct {
	kind     reconciliationActionKind
	resource maskinportenResource
}

func (a *reconciliationAction) String() string {
	return fmt.Sprintf("kind='%s', resource='%s'", a.kind.String(), a.resource.String())
}

type reconciliationActionList []*reconciliationAction

func (l *reconciliationActionList) Strings() []string {
	var res []string
	for _, a := range *l {
		res = append(res, a.String())
	}
	return res
}
