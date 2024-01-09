using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process;

public class ProcessTaskLockingCommonLogic
{
    private readonly IAppMetadata _appMetadata;
    private readonly IDataClient _dataClient;

    public ProcessTaskLockingCommonLogic(IAppMetadata appMetadata, IDataClient dataClient)
    {
        _appMetadata = appMetadata;
        _dataClient = dataClient;
    }

    public async Task UnlockConnectedDataTypes(string taskId, Instance instance)
    {
        var applicationMetadata = await  _appMetadata.GetApplicationMetadata();
        var connectedDataTypes = applicationMetadata.DataTypes.FindAll(dt => dt.TaskId == taskId);
        var instanceIdentifier = new InstanceIdentifier(instance);
        foreach (var dataType in connectedDataTypes)
        {
            var dataElements = instance.Data.FindAll(de => de.DataType == dataType.Id);
            foreach (var dataElement in dataElements)
            {
                await _dataClient.UnlockDataElement(instanceIdentifier, Guid.Parse(dataElement.Id));
            }
        }
    }

    public async Task LockConnectedDataTypes(string taskId, Instance instance)
    {
        var applicationMetadata = await _appMetadata.GetApplicationMetadata();
        var connectedDataTypes = applicationMetadata.DataTypes.FindAll(dt => dt.TaskId == taskId);
        var instanceIdentifier = new InstanceIdentifier(instance);
        foreach (var dataType in connectedDataTypes)
        {
            var dataElements = instance.Data.FindAll(de => de.DataType == dataType.Id);
            foreach (var dataElement in dataElements)
            {
                await _dataClient.LockDataElement(instanceIdentifier, Guid.Parse(dataElement.Id));
            }
        }
    }
}