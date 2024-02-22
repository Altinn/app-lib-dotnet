using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Validation;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Result;
using Altinn.Platform.Storage.Interface.Models;
using Json.Patch;

namespace Altinn.App.Core.Internal.Patch;

/// <summary>
/// Service for applying patches to form data elements
/// </summary>
public class PatchService: IPatchService
{
    private readonly IAppMetadata _appMetadata;
    private readonly IDataClient _dataClient;
    private readonly IAppModel _appModel;
    private readonly IValidationService _validationService;
    private readonly IEnumerable<IDataProcessor> _dataProcessors;
    private readonly IInstanceClient _instanceClient;
    
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Creates a new instance of the <see cref="PatchService"/> class
    /// </summary>
    /// <param name="appMetadata"></param>
    /// <param name="dataClient"></param>
    /// <param name="validationService"></param>
    /// <param name="dataProcessors"></param>
    /// <param name="instanceClient"></param>
    /// <param name="appModel"></param>
    public PatchService(IAppMetadata appMetadata, IDataClient dataClient, IValidationService validationService, IEnumerable<IDataProcessor> dataProcessors, IInstanceClient instanceClient, IAppModel appModel)
    {
        _appMetadata = appMetadata;
        _dataClient = dataClient;
        _validationService = validationService;
        _dataProcessors = dataProcessors;
        _instanceClient = instanceClient;
        _appModel = appModel;
    }

    /// <inheritdoc />
    public async Task<Result<DataPatchResult, DataPatchError>> ApplyPatch(Instance instance, DataType dataType,
        DataElement dataElement, JsonPatch jsonPatch, string? language, List<string>? ignoredValidators = null)
    {
        InstanceIdentifier instanceIdentifier = new InstanceIdentifier(instance);
        AppIdentifier appIdentifier = (await _appMetadata.GetApplicationMetadata()).AppIdentifier;
        var modelType = _appModel.GetModelType(dataType.AppLogic.ClassRef);
        var oldModel =
            await _dataClient.GetFormData(instanceIdentifier.InstanceGuid, modelType, appIdentifier.Org, appIdentifier.App, instanceIdentifier.InstanceOwnerPartyId, Guid.Parse(dataElement.Id));
        var oldModelNode = JsonSerializer.SerializeToNode(oldModel);
            var patchResult = jsonPatch.Apply(oldModelNode);
            if (!patchResult.IsSuccess)
            {
                bool testOperationFailed = patchResult.Error!.Contains("is not equal to the indicated value.");
                return Result<DataPatchResult, DataPatchError>.Err(new DataPatchError()
                {
                    Title = testOperationFailed ? "Precondition in patch failed" : "Patch Operation Failed",
                    Detail = patchResult.Error,
                    Status = testOperationFailed
                        ? DataPatchErrorStatus.PatchTestFailed
                        : DataPatchErrorStatus.DeserializationFailed,
                    Extensions = new Dictionary<string, object?>()
                    {
                        { "previousModel", oldModel },
                        { "patchOperationIndex", patchResult.Operation },
                    }
                });
            }

            var (model, error) = DeserializeModel(oldModel.GetType(), patchResult.Result!);
            if (error is not null)
            {
                return Result<DataPatchResult, DataPatchError>.Err(new DataPatchError()
                {
                    Title = "Patch operation did not deserialize",
                    Detail = error,
                    Status = DataPatchErrorStatus.DeserializationFailed
                });
            }
            Guid dataElementId = Guid.Parse(dataElement.Id);
            foreach (var dataProcessor in _dataProcessors)
            {
                await dataProcessor.ProcessDataWrite(instance, dataElementId, model, oldModel, language);
            }

            // Ensure that all lists are changed from null to empty list.
            ObjectUtils.InitializeListsAndNullEmptyStrings(model);

            var validationIssues = await _validationService.ValidateFormData(instance, dataElement, dataType, model, oldModel, ignoredValidators, language);
            
            await UpdatePresentationTextsOnInstance(instance, dataType.Id, model);
            await UpdateDataValuesOnInstance(instance, dataType.Id, model);

            // Save Formdata to database
            await _dataClient.UpdateData(
                model,
                instanceIdentifier.InstanceGuid,
                modelType,
                appIdentifier.Org,
                appIdentifier.App,
                instanceIdentifier.InstanceOwnerPartyId,
                dataElementId);

            return Result<DataPatchResult, DataPatchError>.Ok(new DataPatchResult
            {
                NewDataModel = model,
                ValidationIssues = validationIssues
            });
    }
    
    private static (object Model, string? Error) DeserializeModel(Type type, JsonNode patchResult)
    {
        try
        {
            var model = patchResult.Deserialize(type, JsonSerializerOptions);
            if (model is null)
            {
                return (null!, "Deserialize patched model returned null");
            }

            return (model, null);
        }
        catch (JsonException e) when (e.Message.Contains("could not be mapped to any .NET member contained in type"))
        {
            // Give better feedback when the issue is that the patch contains a path that does not exist in the model
            return (null!, e.Message);
        }
    }
    
    private async Task UpdatePresentationTextsOnInstance(Instance instance, string dataType, object serviceModel)
    {
        var updatedValues = DataHelper.GetUpdatedDataValues(
            (await _appMetadata.GetApplicationMetadata()).PresentationFields,
            instance.PresentationTexts,
            dataType,
            serviceModel);

        if (updatedValues.Count > 0)
        {
            await _instanceClient.UpdatePresentationTexts(
                int.Parse(instance.Id.Split("/")[0]),
                Guid.Parse(instance.Id.Split("/")[1]),
                new PresentationTexts { Texts = updatedValues });
        }
    }

    private async Task UpdateDataValuesOnInstance(Instance instance, string dataType, object serviceModel)
    {
        var updatedValues = DataHelper.GetUpdatedDataValues(
            (await _appMetadata.GetApplicationMetadata()).DataFields,
            instance.DataValues,
            dataType,
            serviceModel);

        if (updatedValues.Count > 0)
        {
            await _instanceClient.UpdateDataValues(
                int.Parse(instance.Id.Split("/")[0]),
                Guid.Parse(instance.Id.Split("/")[1]),
                new DataValues { Values = updatedValues });
        }
    }
}