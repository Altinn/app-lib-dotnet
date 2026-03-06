using System.Text.Json;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.WorkflowEngine.Models;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.WorkflowEngine;

/// <summary>
/// Service for capturing and restoring instance state for transport between app and workflow engine.
/// </summary>
internal sealed class WorkflowStateSnapshotService
{
    private readonly InstanceDataUnitOfWorkInitializer _unitOfWorkInitializer;
    private readonly ModelSerializationService _modelSerializationService;
    private readonly IAppMetadata _appMetadata;
    private readonly IAppModel _appModel;

    public WorkflowStateSnapshotService(
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
    /// Captures the current state of the unit of work into a typed snapshot for transport.
    /// </summary>
    public async Task<WorkflowStateSnapshot> CaptureSnapshot(InstanceDataUnitOfWork unitOfWork)
    {
        var rawFormData = await unitOfWork.CaptureFormData(_modelSerializationService);
        var formData = rawFormData
            .Select(x => new FormDataEntry
            {
                Id = x.Id,
                DataType = x.DataType,
                Data = x.Data,
            })
            .ToList();
        // Deep-copy the instance so the snapshot is frozen against later mutations.
        // ProcessEngine.HandleMoveToNext captures the snapshot and then mutates instance.Process;
        // without a copy, the snapshot would see the mutated state.
        Instance frozenInstance = JsonSerializer.Deserialize<Instance>(JsonSerializer.Serialize(unitOfWork.Instance))!;

        return new WorkflowStateSnapshot { Instance = frozenInstance, FormData = formData };
    }

    /// <summary>
    /// Restores an InstanceDataUnitOfWork from a previously captured snapshot.
    /// </summary>
    public async Task<InstanceDataUnitOfWork> RestoreSnapshot(WorkflowStateSnapshot snapshot, string? language)
    {
        Instance instance = snapshot.Instance;
        string? taskId = instance.Process?.CurrentTask?.ElementId;

        InstanceDataUnitOfWork unitOfWork = await _unitOfWorkInitializer.Init(
            instance,
            taskId,
            language,
            StorageAuthenticationMethod.ServiceOwner()
        );

        ApplicationMetadata applicationMetadata = await _appMetadata.GetApplicationMetadata();

        foreach (FormDataEntry entry in snapshot.FormData)
        {
            DataElement? dataElement = instance.Data.Find(d => d.Id == entry.Id);
            if (dataElement is null)
                continue;

            DataType? dataType = applicationMetadata.DataTypes.Find(dt => dt.Id == dataElement.DataType);
            if (dataType?.AppLogic?.ClassRef is not { } classRef)
                continue;

            Type modelType = _appModel.GetModelType(classRef);
            byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(entry.Data);
            object model = _modelSerializationService.DeserializeJson(jsonBytes, modelType);
            IFormDataWrapper wrapper = FormDataWrapperFactory.Create(model);

            (ReadOnlyMemory<byte> storageBytes, _) = _modelSerializationService.SerializeToStorage(
                model,
                dataType,
                dataElement
            );

            DataElementIdentifier identifier = dataElement;
            unitOfWork.PreloadFormData(identifier, wrapper);
            unitOfWork.PreloadBinaryData(identifier, storageBytes);
        }

        return unitOfWork;
    }

    /// <summary>
    /// Serializes a snapshot to an opaque JSON string for transport to/from the workflow engine.
    /// </summary>
    public static string Serialize(WorkflowStateSnapshot snapshot) => JsonSerializer.Serialize(snapshot);

    /// <summary>
    /// Deserializes an opaque JSON string back into a typed snapshot.
    /// </summary>
    public static WorkflowStateSnapshot Deserialize(string state) =>
        JsonSerializer.Deserialize<WorkflowStateSnapshot>(state)
        ?? throw new InvalidOperationException("Failed to deserialize workflow state snapshot from callback payload");
}
