using Altinn.App.Core.Features.Correspondence.Exceptions;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Maskinporten;
using Altinn.App.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Features.Correspondence;

[Obsolete("Replaced by CorrespondenceAuthenticationMethod")]
internal sealed class CorrespondenceAuthorizationFactory
{
    private IMaskinportenClient? _maskinportenClient;
    private readonly IServiceProvider _serviceProvider;

    public Func<string, Task<JwtToken>> Maskinporten =>
        async (scope) =>
        {
            _maskinportenClient ??= _serviceProvider.GetRequiredService<IMaskinportenClient>();
            return await _maskinportenClient.GetAltinnExchangedToken([CorrespondenceApiScopes.ServiceOwner, scope]);
        };

    public CorrespondenceAuthorizationFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<JwtToken> Resolve(CorrespondencePayloadBase payload)
    {
        if (payload.AccessTokenFactory is null && payload.AuthorizationMethod is null)
        {
            throw new CorrespondenceArgumentException(
                "Neither AccessTokenFactory nor AuthorizationMethod was provided in the CorrespondencePayload object"
            );
        }

        if (payload.AccessTokenFactory is not null)
        {
            return await payload.AccessTokenFactory();
        }

        return payload.AuthorizationMethod switch
        {
            CorrespondenceAuthorization.Maskinporten => await Maskinporten(payload.RequiredScope),
            _ => throw new CorrespondenceArgumentException(
                $"Unknown CorrespondenceAuthorization `{payload.AuthorizationMethod}`"
            ),
        };
    }
}
