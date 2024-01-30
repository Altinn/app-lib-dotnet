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

        /// <inheritdoc/>
        public DataService(IDataClient dataClient, IAppMetadata appMetadata)
        {
            _dataClient = dataClient;
            _appMetadata = appMetadata;
        }

        /// <inheritdoc/>
        public async Task<(Guid dataElementId, T? model)> GetByType<T>(Instance instance, string dataTypeId)
        {
            var dataElement = instance.Data.SingleOrDefault(d => d.DataType.Equals(dataTypeId));

            if (dataElement == null)
            {
                return (Guid.Empty, default);
            }

            T data = await GetDataForDataElement<T>(instance, dataElement);

            return (Guid.Parse(dataElement.Id), data);
        }

        /// <inheritdoc/>
        public async Task<T> GetById<T>(Instance instance, Guid dataElementId)
        {
            var dataElement = instance.Data.SingleOrDefault(d => d.Id == dataElementId.ToString()) ?? throw new ArgumentException("Failed to locate data element");
            return await GetDataForDataElement<T>(instance, dataElement);
        }

        /// <inheritdoc/>
        public async Task<DataElement> InsertObjectAsJson(InstanceIdentifier instanceIdentifier, string dataTypeId, object data)
        {
            using var referenceStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(referenceStream, data, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            referenceStream.Position = 0;
            return await _dataClient.InsertBinaryData(instanceIdentifier.ToString(), dataTypeId, "application/json", dataTypeId + ".json", referenceStream);
            //return await _dataClient.InsertFormData<PaymentInformation>(instance, dataTypeId, data as PaymentInformation, typeof(PaymentInformation));
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteById(Instance instance, Guid dataElementId)
        {
            var instanceIdentifier = new InstanceIdentifier(instance);
            var applicationMetadata = await _appMetadata.GetApplicationMetadata();

            return await _dataClient.DeleteData(applicationMetadata.AppIdentifier.Org, applicationMetadata.AppIdentifier.App, instanceIdentifier.InstanceOwnerPartyId, instanceIdentifier.InstanceGuid, dataElementId, false);
        }

        private async Task<T> GetDataForDataElement<T>(Instance instance, DataElement dataElement)
        {
            var instanceIdentifier = new InstanceIdentifier(instance);
            var applicationMetadata = await _appMetadata.GetApplicationMetadata();

            var data = await _dataClient.GetFormData(instanceIdentifier.InstanceGuid, typeof(T), applicationMetadata.AppIdentifier.Org, applicationMetadata.AppIdentifier.App, instanceIdentifier.InstanceOwnerPartyId,
                new Guid(dataElement.Id));

            if (data is T model)
            {
                return model;
            }
            else
            {
                throw new ArgumentException($"Failed to locate data element of type {nameof(T)}");
            }
        }
    }
}
