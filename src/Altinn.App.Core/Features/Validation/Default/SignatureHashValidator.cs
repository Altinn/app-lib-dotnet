using System.Diagnostics;
using System.Security.Cryptography;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Features.Signing.Services;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Result;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Validation.Default;

/// <summary>
/// Validates that signature hashes are still valid.
/// </summary>
internal sealed class SignatureHashValidator(
    ISigningService signingService,
    IProcessReader processReader,
    IAppMetadata appMetadata,
    IDataClient dataClient,
    ILogger<SignatureHashValidator> logger
) : IValidator
{
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
            taskConfig = processReader.GetAltinnTaskExtension(taskId);
        }
        catch (Exception)
        {
            return false;
        }

        return taskConfig?.TaskType is "signing"; //TODO: Do you agree that it's best to always run this validator for singing, even if they haven't turned on 'RunDefaultValidator'? Why would you want to finish the step with invalid hashes?
    }

    public bool NoIncrementalValidation => true;

    /// <inheritdoc />
    public Task<bool> HasRelevantChanges(IInstanceDataAccessor dataAccessor, string taskId, DataElementChanges changes)
    {
        throw new UnreachableException(
            "HasRelevantChanges should not be called because NoIncrementalValidation is true."
        );
    }

    public async Task<List<ValidationIssue>> Validate(
        IInstanceDataAccessor dataAccessor,
        string taskId,
        string? language
    )
    {
        Instance instance = dataAccessor.Instance;
        var instanceIdentifier = new InstanceIdentifier(instance);

        AltinnSignatureConfiguration signingConfiguration =
            (processReader.GetAltinnTaskExtension(taskId)?.SignatureConfiguration)
            ?? throw new ApplicationConfigException("Signing configuration not found in AltinnTaskExtension");

        ServiceResult<ApplicationMetadata, Exception> appMetadataResult = await CatchError(
            appMetadata.GetApplicationMetadata
        );

        if (!appMetadataResult.Success)
        {
            logger.LogError(appMetadataResult.Error, "Error while fetching application metadata");
            return [];
        }

        ServiceResult<List<SigneeContext>, Exception> signeeContextsResults = await CatchError(() =>
            signingService.GetSigneeContexts(dataAccessor, signingConfiguration, CancellationToken.None)
        );

        if (!signeeContextsResults.Success)
        {
            logger.LogError(signeeContextsResults.Error, "Error while fetching signee contexts");
            return [];
        }

        foreach (SigneeContext signeeContext in signeeContextsResults.Ok)
        {
            List<SignDocument.DataElementSignature> dataElementSignatures =
                signeeContext.SignDocument?.DataElementSignatures ?? [];

            foreach (SignDocument.DataElementSignature dataElementSignature in dataElementSignatures)
            {
                Stream dataStream = await dataClient.GetBinaryData(
                    instance.Org,
                    instance.AppId,
                    instanceIdentifier.InstanceOwnerPartyId,
                    instanceIdentifier.InstanceGuid,
                    Guid.Parse(dataElementSignature.DataElementId)
                );

                string sha256Hash = await GenerateSha256Hash(dataStream);

                if (sha256Hash != dataElementSignature.Sha256Hash)
                {
                    return
                    [
                        new ValidationIssue
                        {
                            Code = ValidationIssueCodes.DataElementCodes.InvalidSignatureHash,
                            Severity = ValidationIssueSeverity.Error,
                            Description = ValidationIssueCodes.DataElementCodes.InvalidSignatureHash,
                        },
                    ];
                }
            }
        }

        return [];
    }

    private static async Task<string> GenerateSha256Hash(Stream stream)
    {
        using var sha256 = SHA256.Create();
        byte[] digest = await sha256.ComputeHashAsync(stream);
        return FormatShaDigest(digest);
    }

    /// <summary>
    /// Formats a SHA digest with common best best practice:<br/>
    /// Lowercase hexadecimal representation without delimiters
    /// </summary>
    /// <param name="digest">The hash code (digest) to format</param>
    /// <returns>String representation of the digest</returns>
    private static string FormatShaDigest(byte[] digest)
    {
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    /// <summary>
    /// Catch exceptions from an async function and return them as a ServiceResult record with the result.
    /// </summary>
    private static async Task<ServiceResult<T, Exception>> CatchError<T>(Func<Task<T>> function)
    {
        try
        {
            var result = await function();
            return result;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
