using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

internal interface ISigningService
{
    Task<SigneesResult?> GetSignees(Instance instance, AltinnSignatureConfiguration signatureConfiguration);

    Task<List<SigneeContext>> InitializeSignees(
        IInstanceDataMutator instanceMutator,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    );

    Task<List<SigneeContext>> ProcessSignees(
        IInstanceDataMutator instanceMutator,
        List<SigneeContext> signeeContexts,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    );

    Task<List<SigneeContext>> GetSigneeContexts(Instance instance, AltinnSignatureConfiguration signatureConfiguration);
}
