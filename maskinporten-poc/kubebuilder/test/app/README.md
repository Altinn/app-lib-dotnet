## Test project to use with local cluster

```sh
# Creates cluster, installs CRDs
make create
# Installs test application (not the operator)
make deploy

# Run operator, e.g. using the launch profile in VSCode or using `make run` in the root folder

# Creates a CRD instance based on the sample in maskinporten-poc/kubebuilder/config/samples/client_v1_maskinportenclient.yaml
make client

# Do some testing

make destroy
# Deletes the cluster
```