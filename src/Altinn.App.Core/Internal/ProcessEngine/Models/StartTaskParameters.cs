using Altinn.App.Core.Features;

namespace Altinn.App.Core.Internal.ProcessEngine.Models;

public sealed class StartTaskParameters
{
    public required IInstanceDataMutator InstanceDataMutator { get; init; }
}
