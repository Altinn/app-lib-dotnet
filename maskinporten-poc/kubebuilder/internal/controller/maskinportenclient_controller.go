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

	"github.com/go-logr/logr"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/codes"
	"go.opentelemetry.io/otel/trace"
	metav1 "k8s.io/apimachinery/pkg/apis/meta/v1"
	"k8s.io/apimachinery/pkg/runtime"
	ctrl "sigs.k8s.io/controller-runtime"
	"sigs.k8s.io/controller-runtime/pkg/client"
	"sigs.k8s.io/controller-runtime/pkg/controller/controllerutil"
	"sigs.k8s.io/controller-runtime/pkg/log"
	"sigs.k8s.io/controller-runtime/pkg/predicate"

	corev1 "k8s.io/api/core/v1"

	clientv1 "altinn.operator/maskinporten/api/v1"
	"altinn.operator/maskinporten/internal"

	"altinn.operator/maskinporten/internal/maskinporten/api"
	rt "altinn.operator/maskinporten/internal/runtime"
)

const JsonFileName = "maskinporten-client.json"
const FinalizerName = "client.altinn.operator/finalizer"

// MaskinportenClientReconciler reconciles a MaskinportenClient object
type MaskinportenClientReconciler struct {
	client.Client
	Scheme  *runtime.Scheme
	runtime rt.Runtime
	tracer  trace.Tracer
}

func NewMaskinportenClientReconciler(client client.Client, scheme *runtime.Scheme) *MaskinportenClientReconciler {
	rt, err := internal.NewRuntime("")
	if err != nil {
		panic(err)
	}

	return &MaskinportenClientReconciler{
		Client:  client,
		Scheme:  scheme,
		runtime: rt,
		tracer:  otel.Tracer("maskinporten.MaskinportenClientReconciler"),
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
	ctx, span := r.tracer.Start(
		ctx,
		"Reconcile",
		trace.WithAttributes(attribute.String("namespace", kreq.Namespace), attribute.String("name", kreq.Name)),
	)
	defer span.End()

	log := log.FromContext(ctx)

	log.Info("Reconciling MaskinportenClient")

	req, err := r.mapRequest(ctx, kreq)
	if err != nil {
		span.SetStatus(codes.Error, "mapRequest failed")
		span.RecordError(err)
		return ctrl.Result{}, err
	}

	span.SetAttributes(attribute.String("app_id", req.AppId))

	instance, err := r.getInstance(ctx, req)
	if err != nil {
		span.SetStatus(codes.Error, "getInstance failed")
		span.RecordError(err)
		// TODO: we end up here with NotFound after having cleaned up and removed finalizer
		// why?
		return ctrl.Result{}, client.IgnoreNotFound(err)
	}

	span.SetAttributes(
		attribute.String("request_kind", req.Kind.String()),
		attribute.Int64("generation", instance.GetGeneration()),
	)

	desiredState, err := r.computeDesiredState(ctx, req, instance)
	if err != nil {
		span.SetStatus(codes.Error, "computeDesiredState failed")
		span.RecordError(err)
		r.updateWithError(ctx, log, err, "Failed to compute desired state", instance)
		return ctrl.Result{}, err
	}

	currentState, err := r.fetchCurrentState(ctx, req)
	if err != nil {
		span.SetStatus(codes.Error, "fetchCurrentState failed")
		span.RecordError(err)
		r.updateWithError(ctx, log, err, "Failed to fetch resources", instance)
		return ctrl.Result{}, err
	}

	actions, err := r.reconcile(ctx, currentState, desiredState)
	if err != nil {
		span.SetStatus(codes.Error, "reconcile failed")
		span.RecordError(err)
		r.updateWithError(ctx, log, err, "Failed to compute reconciliation", instance)
		return ctrl.Result{}, err
	}

	if len(actions) == 0 {
		log.Info("No actions taken")
		span.SetStatus(codes.Ok, "reconciled successfully")
		return ctrl.Result{}, nil
	}

	err = r.updateStatus(ctx, req, instance, "reconciled", fmt.Sprintf("Reconciled %d resources", len(actions)))
	if err != nil {
		span.SetStatus(codes.Error, "updateStatus failed")
		span.RecordError(err)
		log.Error(err, "Failed to update MaskinportenClient status")
		return ctrl.Result{}, err
	}

	log.Info("Reconciled MaskinportenClient")

	span.SetStatus(codes.Ok, "reconciled successfully")
	return ctrl.Result{}, nil
}

func (r *MaskinportenClientReconciler) updateStatus(
	ctx context.Context,
	req *maskinportenClientRequest,
	instance *clientv1.MaskinportenClient,
	state string,
	reason string,
) error {
	ctx, span := r.tracer.Start(ctx, "Reconcile.updateStatus")
	defer span.End()

	instance.Status.State = state
	timestamp := metav1.Now()
	instance.Status.LastSynced = &timestamp
	instance.Status.Reason = reason

	updatedFinalizers := false
	if req.Kind == RequestCreateKind {
		updatedFinalizers = controllerutil.AddFinalizer(instance, FinalizerName)
	} else if req.Kind == RequestDeleteKind {
		updatedFinalizers = controllerutil.RemoveFinalizer(instance, FinalizerName)
	}

	if updatedFinalizers {
		return r.Update(ctx, instance)
	} else {
		return r.Status().Update(ctx, instance)
	}
}

func (r *MaskinportenClientReconciler) getInstance(
	ctx context.Context,
	req *maskinportenClientRequest,
) (*clientv1.MaskinportenClient, error) {
	ctx, span := r.tracer.Start(ctx, "Reconcile.getInstance")
	defer span.End()

	instance := &clientv1.MaskinportenClient{}
	if err := r.Get(ctx, req.NamespacedName, instance); err != nil {
		return nil, fmt.Errorf("failed to get MaskinportenClient: %w", err)
	}

	if instance.ObjectMeta.DeletionTimestamp.IsZero() {
		if !controllerutil.ContainsFinalizer(instance, FinalizerName) {
			req.Kind = RequestCreateKind
			if err := r.updateStatus(ctx, req, instance, "recorded", ""); err != nil {
				return nil, err
			}
		} else {
			req.Kind = RequestUpdateKind
		}
	} else {
		req.Kind = RequestDeleteKind
	}

	return instance, nil
}

func (r *MaskinportenClientReconciler) updateWithError(
	ctx context.Context,
	log logr.Logger,
	origError error,
	msg string,
	instance *clientv1.MaskinportenClient,
) error {
	ctx, span := r.tracer.Start(ctx, "Reconcile.updateWithError")
	defer span.End()

	log.Error(origError, "Reconciliation of MaskinportenClient failed", "failure", msg)
	instance.Status.State = "error"
	err := r.Status().Update(ctx, instance)
	if err != nil {
		log.Error(err, "Failed to update MaskinportenClient status when encountering error")
	}

	return err
}

func (r *MaskinportenClientReconciler) computeDesiredState(
	ctx context.Context,
	req *maskinportenClientRequest,
	instance *clientv1.MaskinportenClient,
) (maskinportenResourceList, error) {
	_, span := r.tracer.Start(ctx, "Reconcile.computeDesiredState")
	defer span.End()

	resources := make(maskinportenResourceList, 0)

	var clientInfo *api.ClientInfo
	if req.Kind != RequestDeleteKind {
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

	if req.Kind != RequestDeleteKind {
		clientInfoJson, err := json.Marshal(clientInfo)
		if err != nil {
			return resources, err
		}
		secret.Data[JsonFileName] = clientInfoJson

		resources = append(resources, &maskinportenSecretResource{secret: secret})
	}

	return resources, nil
}

func (r *MaskinportenClientReconciler) fetchCurrentState(
	ctx context.Context,
	req *maskinportenClientRequest,
) (maskinportenResourceList, error) {
	ctx, span := r.tracer.Start(ctx, "Reconcile.fetchCurrentState")
	defer span.End()

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

	clientManager := r.runtime.GetMaskinportenClientManager()
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
		if resources[i].kind() == kind {
			return resources[i]
		}
	}
	return nil
}

func (r *MaskinportenClientReconciler) reconcile(
	ctx context.Context,
	currentState maskinportenResourceList,
	desiredState maskinportenResourceList,
) (reconciliationActionList, error) {
	ctx, span := r.tracer.Start(ctx, "Reconcile.reconcile")
	defer span.End()

	actions := make(reconciliationActionList, 0)
	clientManager := r.runtime.GetMaskinportenClientManager()

	for i := range desiredState {
		resource := desiredState[i]
		currentState := find(resource.kind(), currentState)

		switch resource.kind() {
		case ApiClientKind:
			apiClientResource := resource.(*maskinportenApiClientResource)

			clientInfo, created, err := clientManager.Reconcile(apiClientResource.info)
			if err != nil {
				return actions, err
			}
			if created || !apiClientResource.info.Equal(clientInfo) {
				apiClientResource.info = clientInfo
				actions = append(actions, &reconciliationAction{kind: ActionUpsertKind, resource: apiClientResource})
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
					actions = append(actions, &reconciliationAction{kind: ActionUpsertKind, resource: secretResource})
				}
			}
		}
	}

	for i := range currentState {
		resource := currentState[i]
		desiredResource := find(resource.kind(), desiredState)

		switch resource.kind() {
		case ApiClientKind:
			apiClientResource := resource.(*maskinportenApiClientResource)
			if desiredResource == nil {
				if err := clientManager.Delete(apiClientResource.info.AppId); err != nil {
					return actions, err
				}
				actions = append(actions, &reconciliationAction{kind: ActionDeleteKind, resource: apiClientResource})
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
				actions = append(actions, &reconciliationAction{kind: ActionDeleteKind, resource: secretResource})
			}
		}
	}

	return actions, nil
}

// SetupWithManager sets up the controller with the Manager.
func (r *MaskinportenClientReconciler) SetupWithManager(mgr ctrl.Manager) error {
	return ctrl.NewControllerManagedBy(mgr).
		For(&clientv1.MaskinportenClient{}).
		// Only reconcile on generation change (which does not change when status or metadata change)
		WithEventFilter(predicate.GenerationChangedPredicate{}).
		Complete(r)
}
