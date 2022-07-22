using Altinn.App.Core.Invokers;
using Altinn.App.Services.Interface;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Receivers;

/// <summary>
/// Default handling of instance events
/// </summary>
public class DefaultAppEventReceiver: IAppEventReceiver
{
    private readonly ILogger<DefaultAppEventReceiver> _logger;
    private readonly Application _appMetadata;
    private readonly IInstance _instanceClient;
    private readonly IData _dataClient;

    private readonly string _org;
    private readonly string _app;
    
    public DefaultAppEventReceiver(
        ILogger<DefaultAppEventReceiver> logger, 
        IAppResources resourceService, 
        IInstance instanceClient,
        IData dataClient)
    {
        _logger = logger;
        _appMetadata = resourceService.GetApplication();
        _instanceClient = instanceClient;
        _dataClient = dataClient;
        
        _org = _appMetadata.Org;
        _app = _appMetadata.Id.Split("/")[1];
    }

    /// <inheritdoc />
    public async Task OnStartAppEvent(object? sender, AppEventArgs eventArgs)
    {
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task OnEndAppEvent(object? sender, AppEventArgs eventArgs)
    {
        var endEvent = eventArgs.Event;
        var instance = eventArgs.Instance;
        await AutoDeleteDataElements(instance);

        _logger.LogInformation($"OnEndProcess for {instance.Id}, endEvent: {endEvent}");
    }
    
    private async Task AutoDeleteDataElements(Instance instance)
    {
        List<string> typesToDelete = _appMetadata.DataTypes
            .Where(dt => dt?.AppLogic?.AutoDeleteOnProcessEnd == true).Select(dt => dt.Id).ToList();
        if (typesToDelete.Count == 0)
        {
            return;
        }

        instance = await _instanceClient.GetInstance(instance);
        List<DataElement> elementsToDelete = instance.Data.Where(e => typesToDelete.Contains(e.DataType)).ToList();

        List<Task> deleteTasks = new();
        foreach (DataElement item in elementsToDelete)
        {
            deleteTasks.Add(
                _dataClient.DeleteData(
                    _org,
                    _app,
                    int.Parse(instance.InstanceOwner.PartyId),
                    Guid.Parse(item.InstanceGuid),
                    Guid.Parse(item.Id),
                    true));
        }

        await Task.WhenAll(deleteTasks);
    }
}
