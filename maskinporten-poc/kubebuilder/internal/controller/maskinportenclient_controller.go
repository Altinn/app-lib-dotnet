/*
Copyright 2024.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

package controller

import (
	"context"
	"encoding/json"
	"fmt"
	"reflect"
	"strings"

	"github.com/go-logr/logr"
	"k8s.io/apimachinery/pkg/api/errors"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/apimachinery/pkg/runtime"
	"k8s.io/apimachinery/pkg/types"
	ctrl "sigs.k8s.io/controller-runtime"
	"sigs.k8s.io/controller-runtime/pkg/client"
	"sigs.k8s.io/controller-runtime/pkg/log"

	corev1 "k8s.io/api/core/v1"

	clientv1 "altinn.operator/maskinporten/api/v1"
	"altinn.operator/maskinporten/internal"

	"altinn.operator/maskinporten/internal/maskinporten/api"
	rt "altinn.operator/maskinporten/internal/runtime"
)

const JsonFileName = "maskinporten-client.json"

// MaskinportenClientReconciler reconciles a MaskinportenClient object
type MaskinportenClientReconciler struct {
	client.Client
	Scheme  *runtime.Scheme
	Runtime rt.Runtime
}

func NewMaskinportenClientReconciler(client client.Client, scheme *runtime.Scheme) *MaskinportenClientReconciler {
	rt, err := internal.NewRuntime("")
	if err != nil {
		panic(err)
	}

	return &MaskinportenClientReconciler{
		Client:  client,
		Scheme:  scheme,
		Runtime: rt,
	}
}

//+kubebuilder:rbac:groups=client.altinn.operator,resources=maskinportenclients,verbs=get;list;watch;create;update;patch;delete
//+kubebuilder:rbac:groups=client.altinn.operator,resources=maskinportenclients/status,verbs=get;update;patch
//+kubebuilder:rbac:groups=client.altinn.operator,resources=maskinportenclients/finalizers,verbs=update

// Reconcile is part of the main kubernetes reconciliation loop which aims to
// move the current state of the cluster closer to the desired state.
// TODO(user): Modify the Reconcile function to compare the state specified by
// the MaskinportenClient object against the actual cluster state, and then
// perform operations to make the cluster state reflect the state specified by
// the user.
//
// For more details, check Reconcile and its Result here:
// - https://pkg.go.dev/sigs.k8s.io/controller-runtime@v0.17.0/pkg/reconcile
func (r *MaskinportenClientReconciler) Reconcile(ctx context.Context, kreq ctrl.Request) (ctrl.Result, error) {
	log := log.FromContext(ctx)

	log.Info("Reconciling MaskinportenClient")

	req, err := r.mapRequest(kreq)
	if err != nil {
		return ctrl.Result{}, err
	}

	instance, err := r.getInstance(ctx, req)
	if err != nil {
		return ctrl.Result{}, err
	}

	desiredState, err := r.computeDesiredState(ctx, req, instance)
	if err != nil {
		r.updateWithError(ctx, log, err, "Failed to compute desired state", instance)
		return ctrl.Result{}, err
	}

	currentState, err := r.fetchCurrentState(ctx, req)
	if err != nil {
		r.updateWithError(ctx, log, err, "Failed to fetch resources", instance)
		return ctrl.Result{}, err
	}

	actions, err := r.reconcile(ctx, req, currentState, desiredState)
	if err != nil {
		r.updateWithError(ctx, log, err, "Failed to compute reconciliation", instance)
		return ctrl.Result{}, err
	}

	if len(actions) == 0 {
		log.Info("No actions taken")
		return ctrl.Result{}, nil
	}

	if instance != nil {
		instance.Status.State = "reconciled"
		timestamp := metav1.Now()
		instance.Status.LastSynced = &timestamp
		instance.Status.Reason = fmt.Sprintf("Reconciled %d resources", len(actions))
		err = r.Status().Update(ctx, instance)
		if err != nil {
			log.Error(err, "Failed to update MaskinportenClient status")
			return ctrl.Result{}, err
		}
	}

	log.Info("Reconciled MaskinportenClient")

	return ctrl.Result{}, nil
}

type maskinportenClientRequest struct {
	NamespacedName types.NamespacedName
	Name           string
	Namespace      string
	AppId          string
	AppLabel       string
}

type maskinportenResourceKind int

const (
	ApiClientKind maskinportenResourceKind = iota + 1
	SecretKind
)

type maskinportenResource interface {
	Kind() maskinportenResourceKind
}

type maskinportenResourceList []maskinportenResource

type maskinportenSecretResource struct {
	secret *corev1.Secret
}

func (r *maskinportenSecretResource) Kind() maskinportenResourceKind {
	return SecretKind
}

type maskinportenApiClientResource struct {
	info *api.ClientInfo
}

func (r *maskinportenApiClientResource) Kind() maskinportenResourceKind {
	return ApiClientKind
}

type reconciliationActionKind int

const (
	UpsertKind reconciliationActionKind = iota + 1
	DeleteKind
)

type reconciliationAction struct {
	kind     reconciliationActionKind
	resource maskinportenResource
}

type reconciliationActionList []*reconciliationAction

func (r *MaskinportenClientReconciler) mapRequest(req ctrl.Request) (*maskinportenClientRequest, error) {
	nameSplit := strings.Split(req.Name, "-")
	if len(nameSplit) < 2 {
		return nil, fmt.Errorf("unexpected name format for MaskinportenClient resource: %s", req.Name)
	}
	appId := nameSplit[1]

	operatorContext := r.Runtime.GetOperatorContext()

	return &maskinportenClientRequest{
		NamespacedName: req.NamespacedName,
		Name:           req.Name,
		Namespace:      req.Namespace,
		AppId:          appId,
		AppLabel:       fmt.Sprintf("%s-%s-deployment", operatorContext.Te, appId),
	}, nil
}

func (r *MaskinportenClientReconciler) getInstance(ctx context.Context, req *maskinportenClientRequest) (*clientv1.MaskinportenClient, error) {
	log := log.FromContext(ctx)

	instance := &clientv1.MaskinportenClient{}
	err := r.Get(ctx, req.NamespacedName, instance)
	wasDeleted := errors.IsNotFound(err)
	if err != nil && !wasDeleted {
		log.Error(err, "Failed to get MaskinportenClient")
		return nil, fmt.Errorf("failed to get MaskinportenClient: %w", err)
	}

	if wasDeleted {
		return nil, nil
	}

	return instance, nil
}

func (r *MaskinportenClientReconciler) updateWithError(ctx context.Context, log logr.Logger, origError error, msg string, instance *clientv1.MaskinportenClient) error {
	log.Error(origError, "Reconciliation of MaskinportenClient failed", "failure", msg)
	instance.Status.State = "error"
	err := r.Status().Update(ctx, instance)
	if err != nil {
		log.Error(err, "Failed to update MaskinportenClient status when encountering error")
	}

	return err
}

func (r *MaskinportenClientReconciler) computeDesiredState(_ context.Context, req *maskinportenClientRequest, instance *clientv1.MaskinportenClient) (maskinportenResourceList, error) {
	resources := make(maskinportenResourceList, 0)

	var clientInfo *api.ClientInfo
	if instance != nil {
		clientInfo = &api.ClientInfo{
			AppId:  req.AppId,
			Scopes: instance.Spec.Scopes,
		}
		resources = append(resources, &maskinportenApiClientResource{info: clientInfo})
	}

	f := false
	secret := &corev1.Secret{
		ObjectMeta: metav1.ObjectMeta{
			Name:      req.Name,
			Namespace: req.Namespace,
			Labels: map[string]string{
				"app": req.AppLabel,
			},
		},
		Type:      corev1.SecretTypeOpaque,
		Data:      map[string][]byte{},
		Immutable: &f,
	}

	if instance != nil {
		clientInfoJson, err := json.Marshal(clientInfo)
		if err != nil {
			return resources, err
		}
		secret.Data[JsonFileName] = clientInfoJson

		resources = append(resources, &maskinportenSecretResource{secret: secret})
	}

	return resources, nil
}

func (r *MaskinportenClientReconciler) fetchCurrentState(ctx context.Context, req *maskinportenClientRequest) (maskinportenResourceList, error) {
	resources := make(maskinportenResourceList, 0)

	var secrets corev1.SecretList
	if err := r.List(ctx, &secrets, client.InNamespace(req.Namespace), client.MatchingLabels{"app": req.AppLabel}); err != nil {
		// log.Error(err, "Failed to find app secrets")
		return nil, err
	}
	if len(secrets.Items) > 1 {
		// log.Error(nil, "Unexpected number of secrets found", "count", len(secrets.Items))
		return nil, fmt.Errorf("unexpected number of secrets found: %d", len(secrets.Items))
	}

	if len(secrets.Items) == 1 {
		secret := &secrets.Items[0]
		if secret.Type != corev1.SecretTypeOpaque {
			// log.Info("Unexpected secret type", "type", secret.Type)
			return nil, fmt.Errorf("unexpected secret type: %s (expected Opaque)", secret.Type)
		}
		resources = append(resources, &maskinportenSecretResource{secret: secret})
	}

	clientManager := r.Runtime.GetMaskinportenClientManager()
	clientInfo, err := clientManager.Get(req.AppId)
	if err != nil {
		return nil, err
	}

	if clientInfo != nil {
		resources = append(resources, &maskinportenApiClientResource{info: clientInfo})
	}

	return resources, nil
}

func find(kind maskinportenResourceKind, resources maskinportenResourceList) maskinportenResource {
	for i := range resources {
		if resources[i].Kind() == kind {
			return resources[i]
		}
	}
	return nil
}

func (r *MaskinportenClientReconciler) reconcile(ctx context.Context, _ *maskinportenClientRequest, currentState maskinportenResourceList, desiredState maskinportenResourceList) (reconciliationActionList, error) {
	actions := make(reconciliationActionList, 0)
	clientManager := r.Runtime.GetMaskinportenClientManager()

	for i := range desiredState {
		resource := desiredState[i]
		currentState := find(resource.Kind(), currentState)

		switch resource.Kind() {
		case ApiClientKind:
			apiClientResource := resource.(*maskinportenApiClientResource)

			clientInfo, created, err := clientManager.Reconcile(apiClientResource.info)
			if err != nil {
				return actions, err
			}
			if created || !apiClientResource.info.Equal(clientInfo) {
				apiClientResource.info = clientInfo
				actions = append(actions, &reconciliationAction{kind: UpsertKind, resource: apiClientResource})
			}
		case SecretKind:
			secretResource := resource.(*maskinportenSecretResource)
			if currentState == nil {
				return actions, fmt.Errorf("unexpected missing secret resource")
			} else {
				currentSecretResource := currentState.(*maskinportenSecretResource)
				jsonFile := secretResource.secret.Data[JsonFileName]
				currentJsonFile := currentSecretResource.secret.Data[JsonFileName]
				if !reflect.DeepEqual(jsonFile, currentJsonFile) {
					updatedSecret := currentSecretResource.secret.DeepCopy()
					if updatedSecret.Data == nil {
						updatedSecret.Data = make(map[string][]byte)
					}
					updatedSecret.Data[JsonFileName] = secretResource.secret.Data[JsonFileName]
					secretResource.secret = updatedSecret

					if err := r.Update(ctx, secretResource.secret); err != nil {
						return actions, err
					}
					actions = append(actions, &reconciliationAction{kind: UpsertKind, resource: secretResource})
				}
			}
		}
	}

	for i := range currentState {
		resource := currentState[i]
		desiredResource := find(resource.Kind(), desiredState)

		switch resource.Kind() {
		case ApiClientKind:
			apiClientResource := resource.(*maskinportenApiClientResource)
			if desiredResource == nil {
				if err := clientManager.Delete(apiClientResource.info.AppId); err != nil {
					return actions, err
				}
				actions = append(actions, &reconciliationAction{kind: DeleteKind, resource: apiClientResource})
			}
		case SecretKind:
			secretResource := resource.(*maskinportenSecretResource)
			_, currentlyExists := secretResource.secret.Data[JsonFileName]
			shouldExist := desiredResource != nil
			if !shouldExist && currentlyExists {
				delete(secretResource.secret.Data, JsonFileName)
				if err := r.Update(ctx, secretResource.secret); err != nil {
					return actions, err
				}
				actions = append(actions, &reconciliationAction{kind: DeleteKind, resource: secretResource})
			}
		}
	}

	return actions, nil
}

// SetupWithManager sets up the controller with the Manager.
func (r *MaskinportenClientReconciler) SetupWithManager(mgr ctrl.Manager) error {
	return ctrl.NewControllerManagedBy(mgr).
		For(&clientv1.MaskinportenClient{}).
		Complete(r)
}
