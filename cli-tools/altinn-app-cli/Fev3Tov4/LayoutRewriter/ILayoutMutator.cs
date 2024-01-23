using System.Text.Json.Nodes;

namespace altinn_app_cli.fev3tov4.LayoutRewriter;

abstract class IMutationResult { }

class SkipResult : IMutationResult { }

class DeleteResult : IMutationResult { }

class ErrorResult : IMutationResult
{
    public required string Message { get; set; }
}

class ReplaceResult : IMutationResult
{
    public required JsonObject Component { get; set; }
}

/**
 * Note: The Mutate function receives a clone of the component and can be modified directly, and then returned in ReplaceResult.
 */

abstract class ILayoutMutator
{
    public abstract IMutationResult Mutate(
        JsonObject component,
        Dictionary<string, JsonObject> componentLookup
    );
}
