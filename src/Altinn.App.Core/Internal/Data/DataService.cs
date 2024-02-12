using System.Text.Json;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Payment;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Data
{
    /// <inheritdoc/>
    public class DataService : IDataService
    {
        private readonly IDataClient _dataClient;
        private readonly IAppMetadata _appMetadata;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

        
        /// <summary>
        /// Initializes a new instance of the <see cref="DataService"/> class.
        /// </summary>
        /// <param name="dataClient"></param>
        /// <param name="appMetadata"></param>
        public DataService(IDataClient dataClient, IAppMetadata appMetadata)
        {
            _dataClient = dataClient;
            _appMetadata = appMetadata;
        }

        /// <inheritdoc/>
        public async Task<(Guid dataElementId, T? model)> GetByType<T>(Instance instance, string dataTypeId)
        {
            DataElement? dataElement = instance.Data.SingleOrDefault(d => d.DataType.Equals(dataTypeId));

            if (dataElement == null)
            {
                return (Guid.Empty, default);
            }

            var data = await GetDataForDataElement<T>(new InstanceIdentifier(instance), dataElement);

            return (Guid.Parse(dataElement.Id), data);
        }

        /// <inheritdoc/>
        public async Task<T> GetById<T>(Instance instance, Guid dataElementId)
        {
            DataElement dataElement = instance.Data.SingleOrDefault(d => d.Id == dataElementId.ToString()) ?? throw new ArgumentException("Failed to locate data element");
            return await GetDataForDataElement<T>(new InstanceIdentifier(instance), dataElement);
        }

        /// <inheritdoc/>
        public async Task<DataElement> InsertJsonObject(InstanceIdentifier instanceIdentifier, string dataTypeId, object data)
        {
            using var referenceStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(referenceStream, data, _jsonSerializerOptions);
            referenceStream.Position = 0;
            return await _dataClient.InsertBinaryData(instanceIdentifier.ToString(), dataTypeId, "application/json", dataTypeId + ".json", referenceStream);
        }
        
        public async Task<DataElement> UpdateJsonObject(InstanceIdentifier instanceIdentifier, string dataTypeId, Guid dataElementId, object data)
        {
            using var referenceStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(referenceStream, data, _jsonSerializerOptions);
            referenceStream.Position = 0;
            return await _dataClient.UpdateBinaryData(instanceIdentifier, "application/json", dataTypeId + ".json", dataElementId, referenceStream);
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteById(InstanceIdentifier instanceIdentifier, Guid dataElementId)
        {
            ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();

            return await _dataClient.DeleteData(applicationMetadata.AppIdentifier.Org, applicationMetadata.AppIdentifier.App, instanceIdentifier.InstanceOwnerPartyId, instanceIdentifier.InstanceGuid, dataElementId, false);
        }

        private async Task<T> GetDataForDataElement<T>(InstanceIdentifier instanceIdentifier, DataElement dataElement)
        {
            ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();

            object data = await _dataClient.GetFormData(instanceIdentifier.InstanceGuid, typeof(T), applicationMetadata.AppIdentifier.Org, applicationMetadata.AppIdentifier.App, instanceIdentifier.InstanceOwnerPartyId,
                new Guid(dataElement.Id));

            if (data is T model)
            {
                return model;
            }

            throw new ArgumentException($"Failed to locate data element of type {nameof(T)}");
        }
    }
}
