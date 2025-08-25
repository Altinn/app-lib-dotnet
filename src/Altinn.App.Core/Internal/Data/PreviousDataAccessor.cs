using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Helpers.DataModel;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Texts;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Data;

internal class PreviousDataAccessor : IInstanceDataAccessor
{
    private readonly IInstanceDataAccessor _dataAccessor;
    private readonly string? _taskId;
    private readonly IAppResources _appResources;
    private readonly ModelSerializationService _modelSerializationService;
    private readonly FrontEndSettings _frontEndSettings;
    private readonly string? _language;
    private readonly ITranslationService _translationService;
    private readonly Telemetry? _telemetry;

    public PreviousDataAccessor(
        IInstanceDataAccessor dataAccessor,
        string? taskId,
        IAppResources appResources,
        ITranslationService translationService,
        ModelSerializationService modelSerializationService,
        FrontEndSettings frontEndSettings,
        string? language,
        Telemetry? telemetry
    )
    {
        _dataAccessor = dataAccessor;
        _taskId = taskId;
        _appResources = appResources;
        _translationService = translationService;
        _modelSerializationService = modelSerializationService;
        _frontEndSettings = frontEndSettings;
        _language = language;
        _telemetry = telemetry;
    }

    public Instance Instance => _dataAccessor.Instance;

    public IReadOnlyCollection<DataType> DataTypes => _dataAccessor.DataTypes;

    public async Task<object> GetFormData(DataElementIdentifier dataElementIdentifier)
    {
        var binaryData = await _dataAccessor.GetBinaryData(dataElementIdentifier);
        return _modelSerializationService.DeserializeFromStorage(
            binaryData.Span,
            this.GetDataType(dataElementIdentifier)
        );
    }

    public async Task<IFormDataWrapper> GetFormDataWrapper(DataElementIdentifier dataElementIdentifier)
    {
        var dataModel = await GetFormData(dataElementIdentifier);
        return FormDataWrapperFactory.Create(dataModel);
    }

    public IInstanceDataAccessor GetCleanAccessor(RowRemovalOption rowRemovalOption = RowRemovalOption.SetToNull)
    {
        return new CleanInstanceDataAccessor(
            this,
            _taskId,
            _appResources,
            _translationService,
            _frontEndSettings,
            rowRemovalOption,
            _language,
            _telemetry
        );
    }

    public IInstanceDataAccessor GetPreviousDataAccessor()
    {
        return this;
    }

    public async Task<ReadOnlyMemory<byte>> GetBinaryData(DataElementIdentifier dataElementIdentifier)
    {
        return await _dataAccessor.GetBinaryData(dataElementIdentifier);
    }

    public DataElement GetDataElement(DataElementIdentifier dataElementIdentifier)
    {
        return _dataAccessor.GetDataElement(dataElementIdentifier);
    }
}
