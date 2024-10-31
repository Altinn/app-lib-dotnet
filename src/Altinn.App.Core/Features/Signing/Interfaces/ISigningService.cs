﻿using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Sign;

namespace Altinn.App.Core.Features.Signing.Interfaces;

internal interface ISigningService
{
    Task<List<SigneeContext>> InitializeSignees(string taskId, CancellationToken ct);

    Task<List<SigneeContext>> ProcessSignees(List<SigneeContext> signeeContexts, CancellationToken ct);

    List<Signee> ReadSignees();
}