using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.Instances;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Primitives;
using Prometheus;

namespace Altinn.App.Core.Infrastructure.Clients.Storage;

public class InstanceClientMetricsDecorator : IInstanceClient
{
    private readonly IInstanceClient _instanceClient;
    private static readonly Counter InstancesCreatedCounter = Metrics.CreateCounter("altinn_app_instances_created", "Number of instances created", labelNames: new[] { "result" });
    private static readonly Counter InstancesCompletedCounter = Metrics.CreateCounter("altinn_app_instances_completed", "Number of instances completed", labelNames: new[] { "result" });
    private static readonly Counter InstancesDeletedCounter = Metrics.CreateCounter("altinn_app_instances_deleted", "Number of instances completed", labelNames: new[] { "result", "mode" });

    public InstanceClientMetricsDecorator(IInstanceClient instanceClient)
    {
        _instanceClient = instanceClient;
    }

    public async Task<Instance> GetInstance(string app, string org, int instanceOwnerPartyId, Guid instanceId)
    {
        return await _instanceClient.GetInstance(app, org, instanceOwnerPartyId, instanceId);
    }

    public async Task<Instance> GetInstance(Instance instance)
    {
        return await _instanceClient.GetInstance(instance);
    }

    public async Task<List<Instance>> GetInstances(Dictionary<string, StringValues> queryParams)
    {
        return await _instanceClient.GetInstances(queryParams);
    }

    public async Task<Instance> UpdateProcess(Instance instance)
    {
        return await _instanceClient.UpdateProcess(instance);
    }

    public async Task<Instance> CreateInstance(string org, string app, Instance instanceTemplate)
    {
        var success = false;
        try
        {
            var instance = await _instanceClient.CreateInstance(org, app, instanceTemplate);
            success = true;
            return instance;
        }
        finally
        {
            InstancesCreatedCounter.WithLabels(success ? "success" : "failure").Inc();
        }
    }

    public async Task<Instance> AddCompleteConfirmation(int instanceOwnerPartyId, Guid instanceGuid)
    {
        var success = false;
        try
        {
            var instance = await _instanceClient.AddCompleteConfirmation(instanceOwnerPartyId, instanceGuid);
            success = true;
            return instance;
        }
        finally
        {
            InstancesCompletedCounter.WithLabels(success ? "success" : "failure").Inc();
        }
    }

    public async Task<Instance> UpdateReadStatus(int instanceOwnerPartyId, Guid instanceGuid, string readStatus)
    {
        return await _instanceClient.UpdateReadStatus(instanceOwnerPartyId, instanceGuid, readStatus);
    }

    public async Task<Instance> UpdateSubstatus(int instanceOwnerPartyId, Guid instanceGuid, Substatus substatus)
    {
        return await _instanceClient.UpdateSubstatus(instanceOwnerPartyId, instanceGuid, substatus);
    }

    public async Task<Instance> UpdatePresentationTexts(int instanceOwnerPartyId, Guid instanceGuid, PresentationTexts presentationTexts)
    {
        return await _instanceClient.UpdatePresentationTexts(instanceOwnerPartyId, instanceGuid, presentationTexts);
    }

    public async Task<Instance> UpdateDataValues(int instanceOwnerPartyId, Guid instanceGuid, DataValues dataValues)
    {
        return await _instanceClient.UpdateDataValues(instanceOwnerPartyId, instanceGuid, dataValues);
    }

    public async Task<Instance> DeleteInstance(int instanceOwnerPartyId, Guid instanceGuid, bool hard)
    {
        var success = false;
        try
        {
            var deleteInstance = await _instanceClient.DeleteInstance(instanceOwnerPartyId, instanceGuid, hard);
            success = true;
            return deleteInstance;
        }
        finally
        {
            InstancesDeletedCounter.WithLabels(success ? "success" : "failure", hard ? "hard" : "soft").Inc();
        }
    }
}
