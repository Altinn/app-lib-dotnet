package api

type ClientManager interface {
	Get(appId string) (*ClientInfo, error)
	Reconcile(info *ClientInfo) (*ClientInfo, bool, error)
	Delete(appId string) error
}

type ClientUpsertRequest struct {
	AppId string
}
