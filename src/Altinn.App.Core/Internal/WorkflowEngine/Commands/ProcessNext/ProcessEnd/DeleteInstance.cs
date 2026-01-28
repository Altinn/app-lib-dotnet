using System.Globalization;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine.Commands.ProcessNext.ProcessEnd;

internal sealed class DeleteInstance : IWorkflowEngineCommand
{
    private readonly IInstanceClient _instanceClient;

    public DeleteInstance(IInstanceClient instanceClient)
    {
        _instanceClient = instanceClient;
    }

    public string GetKey()
    {
        throw new NotImplementedException();
    }

    public async Task<ProcessEngineCommandResult> Execute(ProcessEngineCommandContext parameters)
    {
        Instance instance = parameters.InstanceDataMutator.Instance;
        InstanceIdentifier instanceIdentifier = new(instance);

        try
        {
            int instanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId, CultureInfo.InvariantCulture);
            await _instanceClient.DeleteInstance(instanceOwnerPartyId, instanceIdentifier.InstanceGuid, true);

            return new SuccessfulProcessEngineCommandResult();
        }
        catch (Exception ex)
        {
            return new FailedProcessEngineCommandResult(ex);
        }
    }
}
