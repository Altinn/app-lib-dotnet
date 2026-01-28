using System.Globalization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.ProcessEngine.Commands;

internal sealed class DeleteDataElements : IProcessEngineCommand
{
    private readonly IAppMetadata _appMetadata;

    public DeleteDataElements(IAppMetadata appMetadata)
    {
        _appMetadata = appMetadata;
    }

    public string GetKey()
    {
        throw new NotImplementedException();
    }

    public async Task<ProcessEngineCommandResult> Execute(ProcessEngineCommandContext parameters)
    {
        ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();
        List<string> typesToDelete = applicationMetadata
            .DataTypes.Where(dt => dt?.AppLogic?.AutoDeleteOnProcessEnd == true)
            .Select(dt => dt.Id)
            .ToList();

        if (typesToDelete.Count == 0)
        {
            return new SuccessfulProcessEngineCommandResult();
        }

        Instance instance = parameters.InstanceDataMutator.Instance;
        List<DataElement> elementsToDelete = instance.Data.Where(e => typesToDelete.Contains(e.DataType)).ToList();

        foreach (DataElement item in elementsToDelete)
        {
            parameters.InstanceDataMutator.RemoveDataElement(item);
        }

        return new SuccessfulProcessEngineCommandResult();
    }
}
