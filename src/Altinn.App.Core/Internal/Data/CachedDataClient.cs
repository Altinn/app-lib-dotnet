using System.Collections.Concurrent;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Data;

internal class CachedDataClient
{
    private readonly IDataClient _dataClient;
    private readonly IAppMetadata _appMetadata;
    private readonly IAppModel _appModel;
    private readonly LazyCache<string, object> _cache = new();

    internal CachedDataClient(IDataClient dataClient, IAppMetadata appMetadata, IAppModel appModel)
    {
        _dataClient = dataClient;
        _appMetadata = appMetadata;
        _appModel = appModel;
    }

    internal async Task<object> Get(Instance instance, DataElement dataElement)
    {
        return await _cache.GetOrCreate(
            dataElement.Id,
            async _ =>
            {
                var appMetadata = await _appMetadata.GetApplicationMetadata();
                var dataType = appMetadata.DataTypes.Find(d => d.Id == dataElement.DataType);
                if (dataType == null)
                {
                    throw new InvalidOperationException($"Data type {dataElement.DataType} not found in app metadata");
                }

                if (dataType.AppLogic?.ClassRef != null)
                {
                    return await GetFormData(instance, dataElement, dataType);
                }

                return await GetBinaryData(instance, dataElement, dataType);
            }
        );
    }

    internal void Set(DataElement dataElement, object data)
    {
        _cache.Set(dataElement.Id, data);
    }

    /// <summary>
    ///     Simple wrapper around a ConcurrentDictionary using Lazy to ensure that the valueFactory is only called once
    /// </summary>
    /// <typeparam name="TKey">The type of the key in the cache</typeparam>
    /// <typeparam name="TValue">The type of the object to cache</typeparam>
    private class LazyCache<TKey, TValue>
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, Lazy<Task<TValue>>> _cache = new();

        public async Task<TValue> GetOrCreate(TKey key, Func<TKey, Task<TValue>> valueFactory)
        {
            return await _cache.GetOrAdd(key, innerKey => new Lazy<Task<TValue>>(() => valueFactory(innerKey))).Value;
        }

        public void Set(TKey key, TValue data)
        {
            if (!_cache.TryAdd(key, new Lazy<Task<TValue>>(Task.FromResult(data))))
            {
                throw new InvalidOperationException($"Key {key} already exists in cache");
            }
        }
    }

    private async Task<Stream> GetBinaryData(Instance instance, DataElement dataElement, DataType dataType)
    {
        var instanceGuid = Guid.Parse(instance.Id.Split("/")[1]);
        var app = instance.AppId.Split("/")[1];
        var instanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId);
        var data = await _dataClient.GetBinaryData(
            instance.Org,
            app,
            instanceOwnerPartyId,
            instanceGuid,
            Guid.Parse(dataElement.Id)
        );
        return data;
    }

    private async Task<object> GetFormData(Instance instance, DataElement dataElement, DataType dataType)
    {
        var modelType = _appModel.GetModelType(dataType.AppLogic.ClassRef);

        var instanceGuid = Guid.Parse(instance.Id.Split("/")[1]);
        var app = instance.AppId.Split("/")[1];
        var instanceOwnerPartyId = int.Parse(instance.InstanceOwner.PartyId);
        var data = await _dataClient.GetFormData(
            instanceGuid,
            modelType,
            instance.Org,
            app,
            instanceOwnerPartyId,
            Guid.Parse(dataElement.Id)
        );
        return data;
    }
}
