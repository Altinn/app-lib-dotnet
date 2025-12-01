using Altinn.App.Core.Features;
using Altinn.App.Core.Models;
using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.Core.Internal.ProcessEngine.Commands;

internal struct ProcessEngineCommandContext
{
    public AppIdentifier AppId { get; init; }
    public InstanceIdentifier InstanceId { get; init; }

    public IInstanceDataMutator InstanceDataMutator { get; init; }
    public CancellationToken CancellationToken { get; init; }
    public ProcessEngineAppCallbackPayload Payload { get; init; }
}
