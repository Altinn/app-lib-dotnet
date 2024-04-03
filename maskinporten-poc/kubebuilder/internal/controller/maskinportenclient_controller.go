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

	"github.com/go-logr/logr"
	"k8s.io/apimachinery/pkg/runtime"
	ctrl "sigs.k8s.io/controller-runtime"
	"sigs.k8s.io/controller-runtime/pkg/client"
	"sigs.k8s.io/controller-runtime/pkg/log"

	corev1 "k8s.io/api/core/v1"

	clientv1 "altinn.operator/maskinporten/api/v1"

	internalContext "altinn.operator/maskinporten/internal/context"
	"altinn.operator/maskinporten/internal/maskinporten"
)

const JsonFileName = "maskinporten-client.json"

// MaskinportenClientReconciler reconciles a MaskinportenClient object
type MaskinportenClientReconciler struct {
	client.Client
	Scheme *runtime.Scheme
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
func (r *MaskinportenClientReconciler) Reconcile(ctx context.Context, req ctrl.Request) (ctrl.Result, error) {
	log := log.FromContext(ctx)

	log.Info("Reconciling MaskinportenClient")

	instance := &clientv1.MaskinportenClient{}
	if err := r.Get(ctx, req.NamespacedName, instance); err != nil {
		log.Error(err, "Failed to get MaskinportenClient")
		return ctrl.Result{}, client.IgnoreNotFound(err)
	}

	operatorContext, err := internalContext.Discover()
	if err != nil {
		r.updateWithError(ctx, log, err, "Failed to discover operator context", instance)
		return ctrl.Result{}, err
	}

	secret, err := r.fetchResources(ctx, instance, operatorContext)
	if err != nil {
		r.updateWithError(ctx, log, err, "Failed to fetch resources", instance)
		return ctrl.Result{}, err
	}

	clientInfo, err := r.reconcileMaskinportenApi(ctx, instance)
	if err != nil {
		r.updateWithError(ctx, log, err, "Failed to reconcile Maskinporten API", instance)
		return ctrl.Result{}, err
	}

	err = r.reconcileSecret(ctx, clientInfo, secret)
	if err != nil {
		r.updateWithError(ctx, log, err, "Failed to reconcile secret", instance)
		return ctrl.Result{}, err
	}

	instance.Status.State = "reconciled"
	err = r.Status().Update(ctx, instance)
	if err != nil {
		log.Error(err, "Failed to update MaskinportenClient status")
		return ctrl.Result{}, err
	}

	log.Info("Reconciled MaskinportenClient")

	return ctrl.Result{}, nil
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

func (r *MaskinportenClientReconciler) fetchResources(ctx context.Context, instance *clientv1.MaskinportenClient, operatorContext *internalContext.OperatorContext) (*corev1.Secret, error) {
	appLabel := fmt.Sprintf("%s-%s-deployment", operatorContext.Te, instance.Spec.AppId)

	var secrets corev1.SecretList
	if err := r.List(ctx, &secrets, client.InNamespace(instance.Namespace), client.MatchingLabels{"app": appLabel}); err != nil {
		// log.Error(err, "Failed to find app secrets")
		return nil, err
	}
	if len(secrets.Items) != 1 {
		// log.Error(nil, "Unexpected number of secrets found", "count", len(secrets.Items))
		return nil, fmt.Errorf("Unexpected number of secrets found: %d", len(secrets.Items))
	}
	secret := secrets.Items[0]
	if secret.Type != corev1.SecretTypeOpaque {
		// log.Info("Unexpected secret type", "type", secret.Type)
		return nil, fmt.Errorf("Unexpected secret type: %s (expected Opaque)", secret.Type)
	}

	return &secret, nil
}

func (r *MaskinportenClientReconciler) reconcileMaskinportenApi(_ context.Context, instance *clientv1.MaskinportenClient) (*maskinporten.ClientInfo, error) {
	clientInfo := maskinporten.ClientInfo{
		ClientId: "test",
	}

	instance.Status.ClientId = clientInfo.ClientId

	return &clientInfo, nil
}

func (r *MaskinportenClientReconciler) reconcileSecret(ctx context.Context, clientInfo *maskinporten.ClientInfo, secret *corev1.Secret) error {
	if secret.Data != nil {
		delete(secret.Data, JsonFileName)
	}

	clientInfoJson, err := json.Marshal(clientInfo)
	if err != nil {
		return err
	}
	if secret.StringData == nil {
		secret.StringData = make(map[string]string)
	}
	secret.StringData[JsonFileName] = string(clientInfoJson)
	r.Update(ctx, secret)
	return nil
}

// SetupWithManager sets up the controller with the Manager.
func (r *MaskinportenClientReconciler) SetupWithManager(mgr ctrl.Manager) error {
	return ctrl.NewControllerManagedBy(mgr).
		For(&clientv1.MaskinportenClient{}).
		Complete(r)
}
