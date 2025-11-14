using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.ProcessEngine.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Internal.ProcessEngine.Commands;

internal struct ProcessEngineCommandContext
{
    public AppIdentifier AppId { get; init; }
    public InstanceIdentifier InstanceId { get; init; }

    public IInstanceDataMutator InstanceDataMutator { get; init; }
    public CancellationToken CancellationToken { get; init; }
    public ProcessEngineCallbackPayload Payload { get; init; }
}
