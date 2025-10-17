using System.Globalization;
using Altinn.App.Clients.Fiks.Exceptions;
using Altinn.App.Clients.Fiks.Extensions;
using Altinn.App.Clients.Fiks.Factories;
using Altinn.App.Clients.Fiks.FiksArkiv.Models;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Internal.AltinnCdn;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Internal.Texts;
using Altinn.App.Core.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmelding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivConfigResolver : IFiksArkivConfigResolver
{
    private readonly FiksArkivSettings _fiksArkivSettings;
    private readonly IAppMetadata _appMetadata;
    private readonly ITranslationService _translationService;
    private readonly InstanceDataUnitOfWorkInitializer _instanceDataUnitOfWorkInitializer;
    private readonly ILayoutEvaluatorStateInitializer _layoutStateInitializer;
    private readonly ILogger<FiksArkivConfigResolver> _logger;
    private readonly GeneralSettings _generalSettings;
    private readonly IAltinnPartyClient _altinnPartyClient;
    private readonly IAltinnCdnClient _altinnCdnClient;

    public FiksArkivConfigResolver(
        IOptions<FiksArkivSettings> fiksArkivSettings,
        IAppMetadata appMetadata,
        ITranslationService translationService,
        InstanceDataUnitOfWorkInitializer instanceDataUnitOfWorkInitializer,
        ILayoutEvaluatorStateInitializer layoutStateInitializer,
        IOptions<GeneralSettings> generalSettings,
        IAltinnPartyClient altinnPartyClient,
        IAltinnCdnClient altinnCdnClient,
        ILogger<FiksArkivConfigResolver> logger
    )
    {
        _fiksArkivSettings = fiksArkivSettings.Value;
        _appMetadata = appMetadata;
        _translationService = translationService;
        _instanceDataUnitOfWorkInitializer = instanceDataUnitOfWorkInitializer;
        _layoutStateInitializer = layoutStateInitializer;
        _generalSettings = generalSettings.Value;
        _altinnPartyClient = altinnPartyClient;
        _altinnCdnClient = altinnCdnClient;
        _logger = logger;
    }

    public FiksArkivDataTypeSettings PrimaryDocumentSettings =>
        _fiksArkivSettings.Documents?.PrimaryDocument
        ?? throw new FiksArkivConfigurationException("FiksArkivSettings.Documents.PrimaryDocument must be configured");
    public IReadOnlyList<FiksArkivDataTypeSettings> AttachmentSettings =>
        _fiksArkivSettings.Documents?.Attachments ?? [];

    public async Task<string> GetApplicationTitle()
    {
        var appMetadata = await _appMetadata.GetApplicationMetadata();

        return await _translationService.TranslateTextKey("appName", LanguageConst.Nb)
            ?? appMetadata.Title.GetValueOrDefault(LanguageConst.Nb)
            ?? appMetadata.AppIdentifier.App;
    }

    public async Task<FiksArkivDocumentMetadata?> GetConfigMetadata(Instance instance)
    {
        if (_fiksArkivSettings.Metadata is null)
            return null;

        try
        {
            var layoutState = await GetLayoutState(instance);

            var caseFileTitle = await GetBindableConfigValue(
                layoutState,
                instance,
                _fiksArkivSettings.Metadata.CaseFileTitle,
                ParseString
            );
            var journalEntryTitle = await GetBindableConfigValue(
                layoutState,
                instance,
                _fiksArkivSettings.Metadata.JournalEntryTitle,
                ParseString
            );
            var systemId = await GetBindableConfigValue(
                layoutState,
                instance,
                _fiksArkivSettings.Metadata.SystemId,
                ParseString
            );
            var ruleId = await GetBindableConfigValue(
                layoutState,
                instance,
                _fiksArkivSettings.Metadata.RuleId,
                ParseString
            );
            var caseFileId = await GetBindableConfigValue(
                layoutState,
                instance,
                _fiksArkivSettings.Metadata.CaseFileId,
                ParseString
            );

            return new FiksArkivDocumentMetadata(systemId, ruleId, caseFileId, caseFileTitle, journalEntryTitle);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Fiks Arkiv error: {Error}", e.Message);
            throw;
        }

        static string? ParseString(object? data) => data as string;
    }

    public async Task<FiksArkivRecipient> GetRecipient(Instance instance)
    {
        try
        {
            var recipientSettings =
                _fiksArkivSettings.Recipient
                ?? throw new FiksArkivConfigurationException("FiksArkivSettings.Recipient must be configured.");
            var layoutState = await GetLayoutState(instance);

            var accountId =
                await GetBindableConfigValue(layoutState, instance, recipientSettings.FiksAccount, ParseGuid)
                ?? throw new FiksArkivException("FiksArkivSettings.Recipient.FiksAccount must be configured.");
            var identifier =
                await GetBindableConfigValue(layoutState, instance, recipientSettings.Identifier, ParseString)
                ?? throw new FiksArkivException("FiksArkivSettings.Recipient.Identifier must be configured.");
            var name =
                await GetBindableConfigValue(layoutState, instance, recipientSettings.Name, ParseString)
                ?? throw new FiksArkivException("FiksArkivSettings.Recipient.Name must be configured.");
            var orgNumber = await GetBindableConfigValue(
                layoutState,
                instance,
                recipientSettings.OrganizationNumber,
                ParseString
            );

            return new FiksArkivRecipient(accountId, identifier, orgNumber, name);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Fiks Arkiv error: {Error}", e.Message);
            throw;
        }

        static Guid? ParseGuid(object? data) =>
            Guid.TryParse($"{data}", out var parsedGuid)
                ? parsedGuid
                : throw new FiksArkivException($"Could not parse recipient account from data binding: {data}");

        static string? ParseString(object? data) => data as string;
    }

    public string GetCorrelationId(Instance instance) => instance.GetInstanceUrl(_generalSettings);

    public Korrespondansepart? GetRecipientParty(Instance instance, FiksArkivRecipient recipient) =>
        KorrespondansepartFactory.CreateRecipient(
            partyId: recipient.Identifier,
            partyName: recipient.Name,
            organizationId: recipient.OrgNumber,
            reference: GetCorrelationId(instance)
        );

    public async Task<Korrespondansepart> GetServiceOwnerParty()
    {
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        AltinnCdnOrgDetails? orgDetails = null;

        try
        {
            AltinnCdnOrgs altinnCdnOrgs = await _altinnCdnClient.GetOrgs();
            orgDetails = altinnCdnOrgs.Orgs?.GetValueOrDefault(appMetadata.Org);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to get service owner details: {Exception}", e);
        }

        return KorrespondansepartFactory.CreateSender(
            partyId: orgDetails?.Orgnr ?? appMetadata.Org,
            partyName: orgDetails?.Name?.Nb ?? orgDetails?.Name?.Nn ?? orgDetails?.Name?.En ?? appMetadata.Org
        );
    }

    public async Task<Klassifikasjon> GetFormSubmitterClassification(Authenticated auth) =>
        auth switch
        {
            Authenticated.User user => await KlassifikasjonFactory.CreateUser(user),
            Authenticated.SystemUser systemUser => KlassifikasjonFactory.CreateSystemUser(systemUser),
            Authenticated.ServiceOwner serviceOwner => KlassifikasjonFactory.CreateServiceOwner(serviceOwner),
            Authenticated.Org org => KlassifikasjonFactory.CreateOrganization(org),
            _ => throw new FiksArkivException(
                $"Could not determine submitter details from authentication context: {auth}"
            ),
        };

    public async Task<Korrespondansepart?> GetInstanceOwnerParty(Instance instance)
    {
        try
        {
            int partyId = int.Parse(instance.InstanceOwner.PartyId, CultureInfo.InvariantCulture);
            Party? party = await _altinnPartyClient.GetParty(partyId);

            if (party is null)
                return null;

            var correspondenceParty = KorrespondansepartFactory.CreateSender(
                partyId: party.PartyUuid?.ToString() ?? party.PartyId.ToString(CultureInfo.InvariantCulture),
                partyName: party.Name
            );

            if (party.Organization is not null)
            {
                correspondenceParty.AddContactInfo(
                    phoneNumber: party.Organization.TelephoneNumber,
                    mobileNumber: party.Organization.MobileNumber,
                    address: party.Organization.MailingAddress,
                    postcode: party.Organization.MailingPostalCode,
                    city: party.Organization.MailingPostalCity
                );
            }
            else if (party.Person is not null)
            {
                correspondenceParty.AddContactInfo(
                    phoneNumber: party.Person.TelephoneNumber,
                    mobileNumber: party.Person.MobileNumber,
                    address: party.Person.MailingAddress,
                    postcode: party.Person.MailingPostalCode,
                    city: party.Person.MailingPostalCity
                );
            }

            return correspondenceParty;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Could not retrieve party information for {PartyId}: {Exception}",
                instance?.InstanceOwner?.PartyId,
                e
            );
        }

        return null;
    }

    private async Task<LayoutEvaluatorState> GetLayoutState(Instance instance)
    {
        var unitOfWork = await _instanceDataUnitOfWorkInitializer.Init(instance, null, null);
        return await _layoutStateInitializer.Init(unitOfWork, null);
    }

    private static async Task<T?> GetBindableConfigValue<T>(
        LayoutEvaluatorState layoutState,
        Instance instance,
        FiksArkivBindableValue<T>? configValue,
        Func<object?, T?> parser
    )
    {
        if (configValue is null)
            return default;

        if (configValue.Value is not null)
            return configValue.Value;

        var binding =
            configValue.DataModelBinding
            ?? throw new FiksArkivException($"Neither value nor data binding was supplied for config: {configValue}");
        ;
        var dataElement = instance.GetRequiredDataElement(binding.DataType);
        var data = await layoutState.GetModelData(binding, dataElement, null);

        return parser.Invoke(data);
    }
}
