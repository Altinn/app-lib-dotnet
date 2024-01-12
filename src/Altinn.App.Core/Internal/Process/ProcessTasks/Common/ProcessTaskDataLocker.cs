using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ProcessTasks;

/// <summary>
/// Can be used to lock data elements connected to a specific task
/// </summary>
public class ProcessTaskDataLocker : IProcessTaskDataLocker
{
    private readonly IAppMetadata _appMetadata;
    private readonly IDataClient _dataClient;

    /// <summary>
    /// Can be used to lock data elements connected to a specific task
    /// </summary>
    public ProcessTaskDataLocker(IAppMetadata appMetadata, IDataClient dataClient)
    {
        _appMetadata = appMetadata;
        _dataClient = dataClient;
    }

    /// <summary>
    /// Unlock data elements connected to a specific task
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="instance"></param>
    /// <returns></returns>
    public async Task Unlock(string taskId, Instance instance)
    {
        var applicationMetadata = await _appMetadata.GetApplicationMetadata();
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

    /// <summary>
    /// Lock data elements connected to a specific task
    /// </summary>
    /// <param name="taskId"></param>
    /// <param name="instance"></param>
    /// <returns></returns>
    public async Task Lock(string taskId, Instance instance)
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