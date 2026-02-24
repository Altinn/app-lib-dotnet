using System.Text.Json;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.WorkflowEngine.Models;
using Altinn.App.Core.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine;

/// <summary>
/// Service for capturing and restoring instance state for transport between app and workflow engine.
/// </summary>
internal sealed class InstanceStateService
{
    private readonly InstanceDataUnitOfWorkInitializer _unitOfWorkInitializer;
    private readonly ModelSerializationService _modelSerializationService;
    private readonly IAppMetadata _appMetadata;
    private readonly IAppModel _appModel;

    public InstanceStateService(
        InstanceDataUnitOfWorkInitializer unitOfWorkInitializer,
        ModelSerializationService modelSerializationService,
        IAppMetadata appMetadata,
        IAppModel appModel
    )
    {
        _unitOfWorkInitializer = unitOfWorkInitializer;
        _modelSerializationService = modelSerializationService;
        _appMetadata = appMetadata;
        _appModel = appModel;
    }

    /// <summary>
    /// Captures the current state of the unit of work into an opaque JsonElement for transport.
    /// </summary>
    public async Task<JsonElement> CaptureState(InstanceDataUnitOfWork unitOfWork)
    {
        var formData = await unitOfWork.CaptureFormData(_modelSerializationService);
        var instanceState = new InstanceState { Instance = unitOfWork.Instance, FormData = formData };
        return JsonSerializer.SerializeToElement(instanceState);
    }

    /// <summary>
    /// Restores an InstanceDataUnitOfWork from a previously captured state element.
    /// </summary>
    public async Task<InstanceDataUnitOfWork> RestoreState(JsonElement stateElement, string? language)
    {
        var instanceState =
            stateElement.Deserialize<InstanceState>()
            ?? throw new InvalidOperationException("Failed to deserialize instance state from callback payload");

        var instance = instanceState.Instance;
        string? taskId = instance.Process?.CurrentTask?.ElementId;

        var unitOfWork = await _unitOfWorkInitializer.Init(
            instance,
            taskId,
            language,
            StorageAuthenticationMethod.ServiceOwner()
        );

        var applicationMetadata = await _appMetadata.GetApplicationMetadata();

        foreach (var (dataElementId, jsonElement) in instanceState.FormData)
        {
            var dataElement = instance.Data.Find(d => d.Id == dataElementId);
            if (dataElement is null)
                continue;

            var dataType = applicationMetadata.DataTypes.Find(dt => dt.Id == dataElement.DataType);
            if (dataType?.AppLogic?.ClassRef is not { } classRef)
                continue;

            var modelType = _appModel.GetModelType(classRef);
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(jsonElement);
            var model = _modelSerializationService.DeserializeJson(jsonBytes, modelType);
            var wrapper = FormDataWrapperFactory.Create(model);

            var (storageBytes, _) = _modelSerializationService.SerializeToStorage(model, dataType, dataElement);

            DataElementIdentifier identifier = dataElement;
            unitOfWork.PreloadFormData(identifier, wrapper);
            unitOfWork.PreloadBinaryData(identifier, storageBytes);
        }

        return unitOfWork;
    }
}
