using KS.Fiks.Arkiv.Models.V1.Arkivering.Arkivmeldingkvittering;

namespace Altinn.App.Clients.Fiks.Extensions;

internal static class ArkivmeldingKvitteringExtensions
{
    public static bool IsErrorResponse(this ArkivmeldingKvittering arkivmeldingKvittering) =>
        arkivmeldingKvittering.MappeFeilet is not null || arkivmeldingKvittering.RegistreringFeilet is not null;
}
