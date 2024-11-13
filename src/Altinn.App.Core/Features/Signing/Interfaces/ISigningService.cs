﻿using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Signing.Interfaces;

internal interface ISigningService
{
    Task<List<SigneeContext>> InitializeSignees(
        Instance instance,
        AltinnSignatureConfiguration signatureConfiguration,
        CancellationToken ct
    );

    Task<List<SigneeContext>> ProcessSignees(
        Instance instance,
        List<SigneeContext> signeeContexts,
        CancellationToken ct
    );

    List<SigneeContext> ReadSignees();
}
