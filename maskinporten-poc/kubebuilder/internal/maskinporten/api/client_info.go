package api

type ClientInfo struct {
	Id     string   `json:"clientId"`
	AppId  string   `json:"appId"`
	Scopes []string `json:"scopes"`
}

func (c *ClientInfo) Equal(other *ClientInfo) bool {
	if c.AppId != other.AppId {
		return false
	}
	if len(c.Scopes) != len(other.Scopes) {
		return false
	}
	for i, scope := range c.Scopes {
		if scope != other.Scopes[i] {
			return false
		}
	}
	return true
}
