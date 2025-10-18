using Altinn.App.Clients.Fiks.Constants;
using Altinn.App.Clients.Fiks.FiksIO.Models;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Altinn.App.Clients.Fiks.Extensions;

internal static class ServiceProviderExtensions
{
    public static ResiliencePipeline<FiksIOMessageResponse> ResolveResiliencePipeline(this IServiceProvider services)
    {
        return services.GetKeyedService<ResiliencePipeline<FiksIOMessageResponse>>(
                FiksIOConstants.UserDefinedResiliencePipelineId
            )
            ?? services.GetRequiredKeyedService<ResiliencePipeline<FiksIOMessageResponse>>(
                FiksIOConstants.DefaultResiliencePipelineId
            );
    }
}
