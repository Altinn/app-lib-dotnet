package context

type OperatorContext struct {
	Te  string
	Env string
}

func Discover() (*OperatorContext, error) {
	// This should come from the environment/context somewhere
	// there should be 1:1 mapping between TE/env:cluster
	te := "local"
	env := "local"

	return &OperatorContext{Te: te, Env: env}, nil
}
