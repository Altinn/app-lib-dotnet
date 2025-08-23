using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

#nullable enable

namespace TestApp.Shared;

public static class HostedServices
{
    private static IServiceCollection? _services;

    public static void CaptureServiceCollection(IServiceCollection services)
    {
        _services = services;
    }

    public static WebApplication UseHostedServicesMetadataEndpoint(this WebApplication app)
    {
        if (_services is null)
        {
            throw new InvalidOperationException(
                "Service collection not captured. Ensure CaptureServiceCollection is called during service registration"
            );
        }
        app.MapGet(
            "/{org}/{app}/hostedservices",
            async ([FromServices] IServiceProvider serviceProvider) =>
            {
                var hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
                var hostedServicesDescriptors = _services!
                    .Where(sd => sd.ServiceType == typeof(IHostedService))
                    .Select(
                        (sd, i) =>
                            new
                            {
                                IsImplementationFactory = sd.ImplementationFactory is not null,
                                IsImplementationType = sd.ImplementationType is not null,
                                IsImplementationInstance = sd.ImplementationInstance is not null,
                                Lifetime = sd.Lifetime,
                                MaterializedType = hostedServices[i].GetType().FullName,
                            }
                    )
                    .ToArray();
                return Results.Json(hostedServicesDescriptors);
            }
        );

        return app;
    }
}
