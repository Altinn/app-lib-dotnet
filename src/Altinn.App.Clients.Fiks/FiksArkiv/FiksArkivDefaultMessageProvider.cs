using Altinn.App.Clients.Fiks.FiksIO;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using KS.Fiks.Arkiv.Models.V1.Meldingstyper;
using Microsoft.Extensions.Options;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

internal sealed class FiksArkivDefaultMessageProvider : IFiksArkivMessageProvider
{
    private readonly FiksArkivSettings _fiksArkivSettings;
    private readonly IAppMetadata _appMetadata;
    private readonly IDataClient _dataClient;

    public FiksArkivDefaultMessageProvider(
        IOptions<FiksArkivSettings> fiksArkivSettings,
        IAppMetadata appMetadata,
        IDataClient dataClient
    )
    {
        _appMetadata = appMetadata;
        _dataClient = dataClient;
        _fiksArkivSettings = fiksArkivSettings.Value;
    }

    public async Task<FiksIOMessageRequest> CreateMessageRequest(string taskId, Instance instance)
    {
        var recipient = await GetRecipient(instance);
        var attachments = await GetAttachments(instance);
        var instanceId = new InstanceIdentifier(instance.Id);

        // TODO: Build Arkivmelding.xml

        return new FiksIOMessageRequest(
            Recipient: recipient,
            MessageType: FiksArkivMeldingtype.ArkivmeldingOpprett,
            SendersReference: instanceId.InstanceGuid,
            MessageLifetime: TimeSpan.FromDays(14),
            Payload: attachments
        );
    }

    public void ValidateConfiguration()
    {
        if (_fiksArkivSettings.AutoSend is null)
            return;

        if (string.IsNullOrWhiteSpace(_fiksArkivSettings.AutoSend.Recipient))
            throw new Exception("Fiks Arkiv error: Recipient configuration is required for auto-send.");

        if (
            _fiksArkivSettings.AutoSend.Recipient.DoesNotContain('-')
            && _fiksArkivSettings.AutoSend.Recipient.DoesNotContain('.')
        )
            throw new Exception("Fiks Arkiv error: Recipient must be a valid Guid or a data model path.");

        if (string.IsNullOrWhiteSpace(_fiksArkivSettings.AutoSend.AfterTaskId))
            throw new Exception("Fiks Arkiv error: AfterTaskId configuration is required for auto-send.");

        if (_fiksArkivSettings.AutoSend.Attachments is null || _fiksArkivSettings.AutoSend.Attachments.Count == 0)
            throw new Exception("Fiks Arkiv error: Attachments configuration is required for auto-send.");

        if (_fiksArkivSettings.AutoSend.Attachments.Any(x => string.IsNullOrWhiteSpace(x.DataType)))
            throw new Exception("Fiks Arkiv error: Attachments must have DataType set for all entries.");
    }

    private async Task<IReadOnlyList<FiksIOMessagePayload>> GetAttachments(Instance instance)
    {
        var instanceId = new InstanceIdentifier(instance.Id);
        var appMetadata = await _appMetadata.GetApplicationMetadata();

        List<FiksIOMessagePayload> attachments = [];
        foreach (var attachment in _fiksArkivSettings.AutoSend?.Attachments ?? [])
        {
            // This should already have been validated by ValidateConfiguration
            if (string.IsNullOrWhiteSpace(attachment.DataType))
                throw new Exception($"Fiks Arkiv error: Invalid Attachment configuration '{attachment}'");

            List<DataElement> dataElements = instance.Data.Where(x => x.DataType == attachment.DataType).ToList();
            if (dataElements.Count == 0)
                throw new Exception(
                    $"Fiks Arkiv error: No data elements found for Attachment.DataType '{attachment.DataType}'"
                );

            foreach (var dataElement in dataElements)
            {
                if (string.IsNullOrWhiteSpace(attachment.Filename) is false)
                {
                    dataElement.Filename = attachment.Filename;
                }
                else if (string.IsNullOrWhiteSpace(dataElement.Filename))
                {
                    var extension = GetExtensionForMimeType(dataElement.ContentType);
                    dataElement.Filename = $"attachment{extension}";
                }

                attachments.Add(
                    new FiksIOMessagePayload(
                        dataElement.Filename,
                        await _dataClient.GetDataBytes(
                            appMetadata.AppIdentifier.Org,
                            appMetadata.AppIdentifier.App,
                            instanceId.InstanceOwnerPartyId,
                            instanceId.InstanceGuid,
                            Guid.Parse(dataElement.Id)
                        )
                    )
                );
            }
        }

        return EnsureUniqueFilenames(attachments);
    }

    private IReadOnlyList<FiksIOMessagePayload> EnsureUniqueFilenames(List<FiksIOMessagePayload> attachments)
    {
        var hasDuplicateFilenames = attachments
            .GroupBy(x => x.Filename.ToLowerInvariant())
            .Where(x => x.Count() > 1)
            .Select(x => x.ToList());

        foreach (var duplicates in hasDuplicateFilenames)
        {
            for (int i = 0; i < duplicates.Count; i++)
            {
                int uniqueId = i + 1;
                string filename = Path.GetFileNameWithoutExtension(duplicates[i].Filename);
                string extension = Path.GetExtension(duplicates[i].Filename);

                duplicates[i] = duplicates[i] with { Filename = $"{filename}({uniqueId}){extension}" };
            }
        }

        return attachments;
    }

    private Task<Guid> GetRecipient(Instance instance)
    {
        // This should already have been validated by ValidateConfiguration
        if (_fiksArkivSettings.AutoSend?.Recipient is null)
            throw new Exception("Fiks Arkiv error: Recipient is missing from AutoSend settings.");

        // TODO: dynamic dot-pick query with instance data here...?
        var recipient = Guid.Parse(_fiksArkivSettings.AutoSend.Recipient);

        return Task.FromResult(recipient);
    }

    private static string? GetExtensionForMimeType(string? mimeType)
    {
        if (mimeType is null)
            return null;

        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["application/xml"] = ".xml",
            ["text/xml"] = ".xml",
            ["application/pdf"] = ".pdf",
            ["application/json"] = ".json",
        };

        return mapping.GetValueOrDefault(mimeType);
    }

    // private async Task Test()
    // {
    //     ILayoutEvaluatorStateInitializer _layoutStateInit;
    //     IInstanceClient _instanceClient;
    //     Instance instance;
    //     ModelSerializationService _modelSerialization;
    //
    //     IInstanceDataAccessor dataAccessor = new InstanceDataUnitOfWork(
    //         instance,
    //         _dataClient,
    //         _instanceClient,
    //         await _appMetadata.GetApplicationMetadata(),
    //         _modelSerialization
    //     );
    //
    //     LayoutEvaluatorState state = await _layoutStateInit.Init(
    //         dataAccessor,
    //         taskId: null // don't load layout for task
    //     );
    // }
}
