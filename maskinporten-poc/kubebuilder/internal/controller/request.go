package controller

import (
	"context"
	"fmt"
	"strings"

	"k8s.io/apimachinery/pkg/types"
	ctrl "sigs.k8s.io/controller-runtime"
)

type requestKind int

const (
	RequestCreateKind requestKind = iota + 1
	RequestUpdateKind
	RequestDeleteKind
)

var requestKindToString = map[requestKind]string{
	RequestCreateKind: "Create",
	RequestUpdateKind: "Update",
	RequestDeleteKind: "Delete",
}

func (k requestKind) String() string {
	if s, ok := requestKindToString[k]; ok {
		return s
	}
	return "Unknown"
}

type maskinportenClientRequest struct {
	NamespacedName types.NamespacedName
	Name           string
	Namespace      string
	AppId          string
	AppLabel       string
	Kind           requestKind
}

func (r *MaskinportenClientReconciler) mapRequest(
	ctx context.Context,
	req ctrl.Request,
) (*maskinportenClientRequest, error) {
	_, span := r.tracer.Start(ctx, "Reconcile.mapRequest")
	defer span.End()

	nameSplit := strings.Split(req.Name, "-")
	if len(nameSplit) < 2 {
		return nil, fmt.Errorf("unexpected name format for MaskinportenClient resource: %s", req.Name)
	}
	appId := nameSplit[1]

	operatorContext := r.runtime.GetOperatorContext()

	return &maskinportenClientRequest{
		NamespacedName: req.NamespacedName,
		Name:           req.Name,
		Namespace:      req.Namespace,
		AppId:          appId,
		AppLabel:       fmt.Sprintf("%s-%s-deployment", operatorContext.Te, appId),
	}, nil
}
