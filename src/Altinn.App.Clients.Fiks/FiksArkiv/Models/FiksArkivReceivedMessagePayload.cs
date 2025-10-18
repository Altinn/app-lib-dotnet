using System.Text.Json.Serialization;
using Altinn.App.Clients.Fiks.Extensions;
using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;
using KS.Fiks.Arkiv.Models.V1.Feilmelding;

namespace Altinn.App.Clients.Fiks.FiksArkiv.Models;

public abstract record FiksArkivReceivedMessagePayload
{
    [JsonPropertyName("filename")]
    public string Filename { get; }

    [JsonPropertyName("content")]
    public string Content { get; }

    private FiksArkivReceivedMessagePayload(string filename, string content)
    {
        Filename = filename;
        Content = content;
    }

    public sealed record Receipt : FiksArkivReceivedMessagePayload
    {
        [JsonPropertyName("details")]
        public FiksArkivReceipt Details { get; }

        internal Receipt(string filename, string content, ArkivmeldingKvittering archiveReceipt)
            : base(filename, content)
        {
            Details = FiksArkivReceipt.Create(
                archiveReceipt.MappeKvittering as SaksmappeKvittering,
                archiveReceipt.RegistreringKvittering as JournalpostKvittering
            );
        }
    }

    public sealed record Error : FiksArkivReceivedMessagePayload
    {
        [JsonPropertyName("details")]
        public IReadOnlyList<FeilmeldingBase> Details { get; }

        internal Error(string filename, string content, IEnumerable<FeilmeldingBase?> errorDetails)
            : base(filename, content)
        {
            Details = errorDetails.OfType<FeilmeldingBase>().ToList();
        }
    }

    public sealed record Unknown : FiksArkivReceivedMessagePayload
    {
        internal Unknown(string filename, string content)
            : base(filename, content) { }
    };

    public static FiksArkivReceivedMessagePayload Create(string filename, string payload, object? deserializedPayload)
    {
        if (deserializedPayload is ArkivmeldingKvittering archiveReceipt)
        {
            return archiveReceipt.IsErrorResponse()
                ? new Error(filename, payload, [archiveReceipt.MappeFeilet, archiveReceipt.RegistreringFeilet])
                : new Receipt(filename, payload, archiveReceipt);
        }

        if (deserializedPayload is FeilmeldingBase errorDetails)
        {
            return new Error(filename, payload, [errorDetails]);
        }

        return new Unknown(filename, payload);
    }
}
