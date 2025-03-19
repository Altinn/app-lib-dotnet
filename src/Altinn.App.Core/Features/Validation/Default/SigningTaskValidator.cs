using System.Diagnostics;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Result;
using Altinn.App.Core.Models.Validation;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Validation.Default;

/// <summary>
/// Default validator for signing tasks. Validates that all parties have signed the current task.
/// </summary>
internal sealed class SigningTaskValidator : IValidator
{
    private readonly IProcessReader _processReader;
    private readonly ISigningService _signingService;
    private readonly IAppMetadata _appMetadata;
    private readonly ILogger<SigningTaskValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SigningTaskValidator"/> class.
    /// </summary>
    public SigningTaskValidator(
        ILogger<SigningTaskValidator> logger,
        IProcessReader processReader,
        ISigningService signingService,
        IAppMetadata appMetadata
    )
    {
        _logger = logger;
        _processReader = processReader;
        _signingService = signingService;
        _appMetadata = appMetadata;
    }

    /// <summary>
    /// We implement <see cref="ShouldRunForTask"/> instead.
    /// </summary>
    public string TaskId => "*";

    /// <summary>
    /// Only runs for tasks that are of type "signing".
    /// </summary>
    public bool ShouldRunForTask(string taskId)
    {
        AltinnTaskExtension? taskConfig;
        try
        {
            taskConfig = _processReader.GetAltinnTaskExtension(taskId);
        }
        catch (Exception)
        {
            return false;
        }

        AltinnSignatureConfiguration? signingConfiguration = taskConfig?.SignatureConfiguration;

        return signingConfiguration?.RunDefaultValidator is true && taskConfig?.TaskType is "signing";
    }

    public bool NoIncrementalValidation => true;

    /// <inheritdoc />
    public Task<bool> HasRelevantChanges(IInstanceDataAccessor dataAccessor, string taskId, DataElementChanges changes)
    {
        throw new UnreachableException(
            "HasRelevantChanges should not be called because NoIncrementalValidation is true."
        );
    }

    /// <inheritdoc />
    /// <remarks>Validates that all required parties have signed the current task.</remarks>
    public async Task<List<ValidationIssue>> Validate(
        IInstanceDataAccessor dataAccessor,
        string taskId,
        string? language
    )
    {
        AltinnSignatureConfiguration signingConfiguration =
            (_processReader.GetAltinnTaskExtension(taskId)?.SignatureConfiguration)
            ?? throw new ApplicationConfigException("Signing configuration not found in AltinnTaskExtension");

        var appMetadataResult = await CatchError(_appMetadata.GetApplicationMetadata());
        if (!appMetadataResult.Success)
        {
            _logger.LogError(appMetadataResult.Error, "Error while fetching application metadata");
            return [];
        }

        var signeeContextsResult = await CatchError(
            _signingService.GetSigneeContexts(dataAccessor, signingConfiguration)
        );
        if (!signeeContextsResult.Success)
        {
            _logger.LogError(signeeContextsResult.Error, "Error while fetching signee contexts");
            return [];
        }

        bool atLeastOneSignature = signeeContextsResult.Ok.Any(signeeContext => signeeContext.SignDocument is not null);
        bool allSigneesHaveSigned = signeeContextsResult.Ok.All(signeeContext =>
            signeeContext.SignDocument is not null
        );

        if (atLeastOneSignature && allSigneesHaveSigned)
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

    /// <summary>
    /// Catch exceptions from a task and return them as a ServiceResult record with the result.
    /// </summary>
    private static async Task<ServiceResult<T, Exception>> CatchError<T>(Task<T> task)
    {
        try
        {
            var result = await task;
            return result;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
