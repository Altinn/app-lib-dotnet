using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Features.Signing.Exceptions;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Registers;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Logging;
using static Altinn.App.Core.Features.Signing.Models.Internal.Signee;
using JsonException = Newtonsoft.Json.JsonException;

namespace Altinn.App.Core.Features.Signing.Models.Internal;

internal sealed class SignDocumentManager(
    IAltinnPartyClient altinnPartyClient,
    ILogger<SigningService> logger,
    Telemetry? telemetry = null
) : ISignDocumentManager
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(
        new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve,
            MaxDepth = 16,
        }
    );
    private readonly ILogger<SigningService> _logger = logger;

    public async Task<List<SignDocument>> GetSignDocuments(
        IInstanceDataAccessor instanceDataAccessor,
        AltinnSignatureConfiguration signatureConfiguration
    )
    {
        using Activity? activity = telemetry?.StartGetSignDocumentsActivity();
        string signatureDataTypeId =
            signatureConfiguration.SignatureDataType
            ?? throw new ApplicationConfigException("SignatureDataType is not set in the signature configuration.");

        IEnumerable<DataElement> signatureDataElements = instanceDataAccessor.GetDataElementsForType(
            signatureDataTypeId
        );

        try
        {
            SignDocument[] signDocuments = await Task.WhenAll(
                signatureDataElements.Select(signatureDataElement =>
                    DownloadSignDocumentAsync(instanceDataAccessor, signatureDataElement)
                )
            );

            return [.. signDocuments];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download signature documents.");
            throw;
        }
    }

    /// <summary>
    /// This method exists to ensure we have a SigneeContext for both signees that have been delegated access to sign and signees that have signed using access granted through the policy.xml file.
    /// </summary>
    public async Task SynchronizeSigneeContextsWithSignDocuments(
        string taskId,
        List<SigneeContext> signeeContexts,
        List<SignDocument> signDocuments
    )
    {
        _logger.LogDebug(
            "Synchronizing signee contexts {SigneeContexts} with sign documents {SignDocuments} for task {TaskId}.",
            JsonSerializer.Serialize(signeeContexts, _jsonSerializerOptions),
            JsonSerializer.Serialize(signDocuments, _jsonSerializerOptions),
            taskId
        );

        List<SignDocument> unmatchedSignDocuments = signDocuments;

        // OrganizationSignee is most general, so it should be sorted to the end of the list
        signeeContexts.Sort(
            (a, b) =>
                a.Signee is OrganizationSignee ? 1
                : b.Signee is OrganizationSignee ? -1
                : 0
        );

        foreach (SigneeContext signeeContext in signeeContexts)
        {
            SignDocument? matchedSignDocument = signDocuments.FirstOrDefault(signDocument =>
            {
                return signeeContext.Signee switch
                {
                    PersonSignee personSignee => IsPersonSignDocument(signDocument)
                        && personSignee.SocialSecurityNumber == signDocument.SigneeInfo.PersonNumber,
                    PersonOnBehalfOfOrgSignee personOnBehalfOfOrgSignee => IsPersonOnBehalfOfOrgSignDocument(
                        signDocument
                    )
                        && personOnBehalfOfOrgSignee.OnBehalfOfOrg.OrgNumber
                            == signDocument.SigneeInfo.OrganisationNumber
                        && personOnBehalfOfOrgSignee.SocialSecurityNumber == signDocument.SigneeInfo.PersonNumber,
                    SystemSignee systemSignee => IsSystemSignDocument(signDocument)
                        && systemSignee.OnBehalfOfOrg.OrgNumber == signDocument.SigneeInfo.OrganisationNumber
                        && systemSignee.SystemId.Equals(signDocument.SigneeInfo.SystemUserId),
                    OrganizationSignee orgSignee => IsOrgSignDocument(signDocument)
                        && orgSignee.OrgNumber == signDocument.SigneeInfo.OrganisationNumber,

                    _ => throw new InvalidOperationException("Signee is not of a supported type."),
                };
            });

            if (matchedSignDocument is not null)
            {
                if (signeeContext.Signee is OrganizationSignee orgSignee)
                {
                    await ConvertOrgSignee(matchedSignDocument, signeeContext, orgSignee);
                }

                signeeContext.SignDocument = matchedSignDocument;
                unmatchedSignDocuments.Remove(matchedSignDocument);
            }
        }

        // Create new contexts for documents that aren't matched with existing signee contexts
        foreach (SignDocument signDocument in unmatchedSignDocuments)
        {
            SigneeContext newSigneeContext = await CreateSigneeContextFromSignDocument(taskId, signDocument);
            signeeContexts.Add(newSigneeContext);
        }
    }

    private async Task<SignDocument> DownloadSignDocumentAsync(
        IInstanceDataAccessor instanceDataAccessor,
        DataElement signatureDataElement
    )
    {
        try
        {
            ReadOnlyMemory<byte> data = await instanceDataAccessor.GetBinaryData(signatureDataElement);
            string signDocumentSerialized = Encoding.UTF8.GetString(data.ToArray());

            return JsonSerializer.Deserialize<SignDocument>(signDocumentSerialized, _jsonSerializerOptions)
                ?? throw new JsonException("Could not deserialize signature document.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to download signature document for DataElement with ID {DataElementId}.",
                signatureDataElement.Id
            );
            throw;
        }
    }

    // Keep the original method for backward compatibility
    private async Task ConvertOrgSignee(
        SignDocument? signDocument,
        SigneeContext orgSigneeContext,
        OrganizationSignee orgSignee
    )
    {
        if (signDocument is null)
        {
            return;
        }

        var signeeInfo = signDocument.SigneeInfo;

        if (!string.IsNullOrEmpty(signeeInfo.PersonNumber))
        {
            orgSigneeContext.Signee = await orgSignee.ToPersonOnBehalfOfOrgSignee(signeeInfo.PersonNumber, LookupParty);
        }
        else if (signeeInfo.SystemUserId.HasValue)
        {
            orgSigneeContext.Signee = orgSignee.ToSystemSignee(signeeInfo.SystemUserId.Value);
        }
        else
        {
            throw new InvalidOperationException("Signee is neither a person nor a system user");
        }
    }

    private async Task<SigneeContext> CreateSigneeContextFromSignDocument(string taskId, SignDocument signDocument)
    {
        _logger.LogDebug(
            "Creating signee context for sign document {SignDocument} for task {TaskId}.",
            JsonSerializer.Serialize(signDocument, _jsonSerializerOptions),
            taskId
        );

        return new SigneeContext
        {
            TaskId = taskId,
            Signee = await From(
                signDocument.SigneeInfo.PersonNumber,
                signDocument.SigneeInfo.OrganisationNumber,
                signDocument.SigneeInfo.SystemUserId,
                LookupParty
            ),
            SigneeState = new SigneeContextState() { IsAccessDelegated = true, HasBeenMessagedForCallToSign = true },
            SignDocument = signDocument,
        };
    }

    private static bool IsPersonOnBehalfOfOrgSignDocument(SignDocument signDocument)
    {
        return !string.IsNullOrEmpty(signDocument.SigneeInfo.PersonNumber)
            && !string.IsNullOrEmpty(signDocument.SigneeInfo.OrganisationNumber);
    }

    private static bool IsPersonSignDocument(SignDocument signDocument)
    {
        return !string.IsNullOrEmpty(signDocument.SigneeInfo.PersonNumber)
            && string.IsNullOrEmpty(signDocument.SigneeInfo.OrganisationNumber);
    }

    private static bool IsOrgSignDocument(SignDocument signDocument)
    {
        return !string.IsNullOrEmpty(signDocument.SigneeInfo.OrganisationNumber);
    }

    private static bool IsSystemSignDocument(SignDocument signDocument)
    {
        return !string.IsNullOrEmpty(signDocument.SigneeInfo.OrganisationNumber)
            && signDocument.SigneeInfo.SystemUserId.HasValue;
    }

    private async Task<Party> LookupParty(PartyLookup partyLookup)
    {
        try
        {
            return await altinnPartyClient.LookupParty(partyLookup);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to look up party.");
            throw new SigningException("Failed to look up party.");
        }
    }
}
