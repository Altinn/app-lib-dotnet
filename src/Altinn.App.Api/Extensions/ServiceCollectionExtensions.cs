using System;
using Altinn.App.Api.Infrastructure.Telemetry;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Altinn.App.Api.Extensions
{
    /// <summary>
    /// Class for registering requiered services to run an Altinn application.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds all services to run an Altinn application.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> being built.</param>
        /// <param name="configuration">A reference to the current <see cref="IConfiguration"/> object.</param>
        /// <param name="env">A reference to the current <see cref="IWebHostEnvironment"/> object.</param>
        public static void AddAltinnAppServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
        {
            AddApplicationInsights(services, configuration, env);
        }

        private static void AddApplicationInsights(IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
        {
            string applicationInsightsKey = env.IsDevelopment() ?
                         configuration["ApplicationInsights:InstrumentationKey"]
                         : Environment.GetEnvironmentVariable("ApplicationInsights__InstrumentationKey");

            if (!string.IsNullOrEmpty(applicationInsightsKey))
            {
                services.AddApplicationInsightsTelemetry(applicationInsightsKey);
                services.AddApplicationInsightsTelemetryProcessor<IdentityTelemetryFilter>();
                services.AddApplicationInsightsTelemetryProcessor<HealthTelemetryFilter>();
                services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
            }
        }
    }
}