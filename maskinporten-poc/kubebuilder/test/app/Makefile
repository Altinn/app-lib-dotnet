.PHONY: create
create: ## Initialize the test-application infra.
	kind delete cluster --name operator
	kind create cluster --config ./kind.config.yaml
	kubectl config use-context kind-operator
	helm repo add traefik https://traefik.github.io/charts
	sleep 1
	helm upgrade --install --force --version 26.1.0 traefik traefik/traefik --set ports.web.nodePort=30000 --set ports.websecure.nodePort=30001 --set ports.traefik.expose=true --set ports.traefik.nodePort=30002 --set service.type=NodePort
	cd ../../ && make install

.PHONY: build
build: ## Build the local-testapp image.
	kubectl config use-context kind-operator
	docker build -t local-testapp:latest . && helm dep up ./deployment/
	kind load docker-image local-testapp:latest --name operator

.PHONY: deploy
deploy: build ## Deploy test-application to current kube context.
	kubectl config use-context kind-operator
	helm upgrade --install --force local-testapp ./deployment --set deployment.image.repository=local-testapp

.PHONY: client
client: ## Deploy a MaskinportenClient CRD to the cluster.
	kubectl config use-context kind-operator
	kubectl apply -f ../../config/samples/client_v1_maskinportenclient.yaml

.PHONY: delete-client
delete-client: ## Delete a MaskinportenClient CRD on the cluster.
	kubectl config use-context kind-operator
	kubectl delete -f ../../config/samples/client_v1_maskinportenclient.yaml

.PHONY: destroy
destroy: ## Destroy the test-application, including kind cluster.
	kubectl config use-context kind-operator
	helm uninstall --ignore-not-found local-testapp
	kind delete cluster --name operator
