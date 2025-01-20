using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Validation;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Validation.Default;

/// <summary>
/// Validates that all required parties have signed the current task
/// </summary>
internal class SigningTaskValidator : IValidator
{
    private readonly IProcessReader _processReader;
    private readonly ISigningService _signingService;
    private readonly IDataClient _dataClient;
    private readonly IInstanceClient _instanceClient;
    private readonly IAppMetadata _appMetadata;
    private readonly ModelSerializationService _modelSerialization;
    private readonly ILogger<SigningTaskValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SigningTaskValidator"/> class.
    /// </summary>
    public SigningTaskValidator(
        ILogger<SigningTaskValidator> logger,
        IProcessReader processReader,
        ISigningService signingService,
        IDataClient dataClient,
        IInstanceClient instanceClient,
        IAppMetadata appMetadata,
        ModelSerializationService modelSerialization
    )
    {
        _logger = logger;
        _processReader = processReader;
        _signingService = signingService;
        _dataClient = dataClient;
        _instanceClient = instanceClient;
        _appMetadata = appMetadata;
        _modelSerialization = modelSerialization;
    }

    /// <summary>
    /// We implement <see cref="ShouldRunForTask"/> instead
    /// </summary>
    public string TaskId => "*";

    /// <summary>
    /// Only run for tasks that are of type "signing"
    /// </summary>
    public bool ShouldRunForTask(string taskId)
    {
        return _processReader.GetAltinnTaskExtension(taskId)?.TaskType == "signing";
    }

    public bool NoIncrementalValidation => true;

    // Should never be called because NoIncrementalValidation is true
    public Task<bool> HasRelevantChanges(IInstanceDataAccessor dataAccessor, string taskId, DataElementChanges changes)
    {
        throw new NotImplementedException();
    }

    public async Task<List<ValidationIssue>> Validate(
        IInstanceDataAccessor dataAccessor,
        string taskId,
        string? language
    )
    {
        AltinnSignatureConfiguration? signingConfiguration = (
            _processReader.GetAltinnTaskExtension(taskId)?.SignatureConfiguration
        );

        if (signingConfiguration == null)
        {
            _logger.LogError($"No signing configuration found for task {taskId}");
            return [];
        }

        var (getAppMetadataError, appMetadata) = await CatchError(_appMetadata.GetApplicationMetadata());
        if (getAppMetadataError != null || appMetadata == null)
        {
            _logger.LogError(getAppMetadataError, "Error while fetching application metadata");
            return [];
        }

        var cachedDataMutator = new InstanceDataUnitOfWork(
            instance: dataAccessor.Instance,
            _dataClient,
            _instanceClient,
            appMetadata,
            _modelSerialization
        );

        var (getSigneeContextsError, signeeContexts) = await CatchError(
            _signingService.GetSigneeContexts(cachedDataMutator, signingConfiguration)
        );
        if (getSigneeContextsError != null || signeeContexts == null)
        {
            _logger.LogError(getSigneeContextsError, "Error while fetching signee contexts");
            return [];
        }

        var allHaveSigned = signeeContexts.All(signeeContext => signeeContext.SignDocument != null);
        if (allHaveSigned)
        {
            return [];
        }

        return
        [
            new ValidationIssue
            {
                Code = ValidationIssueCodes.DataElementCodes.MissingSignatures,
                Severity = ValidationIssueSeverity.Error,
                Description = ValidationIssueCodes.DataElementCodes.MissingSignatures,
            },
        ];
    }

    public static async Task<Tuple<Exception?, T?>> CatchError<T>(Task<T> task)
    {
        try
        {
            var result = await task;
            return Tuple.Create<Exception?, T?>(null, result);
        }
        catch (Exception ex)
        {
            return Tuple.Create<Exception?, T?>(ex, default);
        }
    }
}
