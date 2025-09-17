using System.Diagnostics;
using System.Security.Cryptography;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Features.Signing.Services;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
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
    IDataClient dataClient,
    IAppMetadata appMetadata,
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

        return taskConfig?.TaskType is "signing";
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

        ApplicationMetadata applicationMetadata = await appMetadata.GetApplicationMetadata();

        List<SigneeContext> signeeContextsResults = await signingService.GetSigneeContexts(
            dataAccessor,
            signingConfiguration,
            CancellationToken.None
        );

        foreach (SigneeContext signeeContext in signeeContextsResults)
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
                    Guid.Parse(dataElementSignature.DataElementId),
                    HasRestrictedRead(applicationMetadata, instance, dataElementSignature.DataElementId)
                        ? StorageAuthenticationMethod.ServiceOwner()
                        : null
                );

                string sha256Hash = await GenerateSha256Hash(dataStream);

                if (sha256Hash != dataElementSignature.Sha256Hash)
                {
                    logger.LogError(
                        $"Found an invalid signature for data element {dataElementSignature.DataElementId} on instance {instance.Id}. Expected hash {dataElementSignature.Sha256Hash}, calculated hash {sha256Hash}."
                    );

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

        logger.LogInformation("All signature hashes are valid for instance {InstanceId}", instance.Id);

        return [];
    }

    private static bool HasRestrictedRead(
        ApplicationMetadata applicationMetadata,
        Instance instance,
        string dataElementId
    )
    {
        DataElement? dataElement = instance.Data.FirstOrDefault(de => de.Id == dataElementId);
        string? dataTypeId = dataElement?.DataType;
        DataType? dataType = applicationMetadata.DataTypes.FirstOrDefault(dt => dt.Id == dataTypeId);

        if (dataType == null)
        {
            throw new ApplicationConfigException(
                $"Unable to find data type {dataTypeId} for data element {dataElementId} in applicationmetadata.json."
            );
        }

        return !string.IsNullOrEmpty(dataType?.ActionRequiredToRead);
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
}
