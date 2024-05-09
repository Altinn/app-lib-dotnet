using Altinn.App.Api.Controllers;
using Altinn.App.Api.Helpers;
using Altinn.App.Api.Infrastructure.Filters;
using Altinn.App.Api.Infrastructure.Health;
using Altinn.App.Api.Infrastructure.Telemetry;
using Altinn.App.Core.Extensions;
using Altinn.App.Core.Features;
using Altinn.Common.PEP.Authorization;
using Altinn.Common.PEP.Clients;
using AltinnCore.Authentication.JwtCookie;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Altinn.App.Api.Extensions
{
    /// <summary>
    /// Class for registering required services to run an Altinn application.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add the controllers and views used by an Altinn application
        /// </summary>
        public static void AddAltinnAppControllersWithViews(this IServiceCollection services)
        {
            // Add API controllers from Altinn.App.Api
            IMvcBuilder mvcBuilder = services.AddControllersWithViews();
            mvcBuilder
                .AddApplicationPart(typeof(InstancesController).Assembly)
                .AddXmlSerializerFormatters()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                });
        }

        /// <summary>
        /// Adds all services to run an Altinn application.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> being built.</param>
        /// <param name="config">A reference to the current <see cref="IConfiguration"/> object.</param>
        /// <param name="env">A reference to the current <see cref="IWebHostEnvironment"/> object.</param>
        public static AltinnAppBuilder AddAltinnAppServices(
            this IServiceCollection services,
            IConfiguration config,
            IWebHostEnvironment env
        )
        {
            services.AddMemoryCache();
            services.AddHealthChecks().AddCheck<HealthCheck>("default_health_check");
            services.AddFeatureManagement();

            services.AddPlatformServices(config, env);
            services.AddAppServices(config, env);
            services.ConfigureDataProtection();

            var useOpenTelemetrySetting = config.GetValue<bool?>("AppSettings:UseOpenTelemetry");
            IOpenTelemetryBuilder? openTelemetryBuilder = null;

            // Use Application Insights as default, opt in to use Open Telemetry
            if (useOpenTelemetrySetting is true)
            {
                openTelemetryBuilder = AddOpenTelemetry(services, config, env);
            }
            else
            {
                AddApplicationInsights(services, config, env);
            }

            AddAuthenticationScheme(services, config, env);
            AddAuthorizationPolicies(services);
            AddAntiforgery(services);

            services.AddSingleton<IAuthorizationHandler, AppAccessHandler>();

            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            // HttpClients for platform functionality. Registered as HttpClient so default HttpClientFactory is used
            services.AddHttpClient<AuthorizationApiClient>();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            var builder = services.GetOrCreateInstanceInServices<AltinnAppBuilder>(
                () => new AltinnAppBuilder(openTelemetryBuilder)
            );

            return builder;
        }

        /// <summary>
        /// Adds Application Insights to the service collection.
        /// </summary>
        /// <param name="services">Services</param>
        /// <param name="config">Config</param>
        /// <param name="env">Environment</param>
        internal static void AddApplicationInsights(
            IServiceCollection services,
            IConfiguration config,
            IWebHostEnvironment env
        )
        {
            var (applicationInsightsKey, applicationInsightsConnectionString) = GetAppInsightsConfig(
                config,
                env.EnvironmentName
            );

            if (
                !string.IsNullOrEmpty(applicationInsightsKey)
                || !string.IsNullOrEmpty(applicationInsightsConnectionString)
            )
            {
                services.AddApplicationInsightsTelemetry(
                    (options) =>
                    {
                        if (string.IsNullOrEmpty(applicationInsightsConnectionString))
                        {
#pragma warning disable CS0618 // Type or member is obsolete
                            // Set instrumentationKey for compatibility if connectionString does not exist.
                            options.InstrumentationKey = applicationInsightsKey;
#pragma warning restore CS0618 // Type or member is obsolete
                        }
                        else
                        {
                            options.ConnectionString = applicationInsightsConnectionString;
                        }
                    }
                );
                services.AddApplicationInsightsTelemetryProcessor<IdentityTelemetryFilter>();
                services.AddApplicationInsightsTelemetryProcessor<HealthTelemetryFilter>();
                services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
            }
        }

        private static IOpenTelemetryBuilder AddOpenTelemetry(
            IServiceCollection services,
            IConfiguration config,
            IWebHostEnvironment env
        )
        {
            var appId = StartupHelper.GetApplicationId().Split("/")[1];
            var appVersion = config.GetSection("AppSettings").GetValue<string>("AppVersion");
            if (string.IsNullOrWhiteSpace(appVersion))
            {
                appVersion = "Local";
            }
            services.AddHostedService<TelemetryInitialization>();
            services.AddSingleton<Telemetry>();

            var (appInsightsKey, appInsightsConnectionString) = GetAppInsightsConfig(config, env.EnvironmentName);
            if (string.IsNullOrWhiteSpace(appInsightsConnectionString) && !string.IsNullOrWhiteSpace(appInsightsKey))
            {
                appInsightsConnectionString = $"InstrumentationKey={appInsightsKey}";
            }

            var builder = services.GetOrCreateInstanceInServices<IOpenTelemetryBuilder>(
                () =>
                    services
                        .AddOpenTelemetry()
                        .ConfigureResource(r =>
                            r.AddService(
                                serviceName: appId,
                                serviceVersion: appVersion,
                                serviceInstanceId: Environment.MachineName
                            )
                        )
                        .WithTracing(builder =>
                        {
                            if (env.IsDevelopment())
                            {
                                builder.SetSampler(new AlwaysOnSampler());
                            }

                            builder = builder
                                .AddSource(appId)
                                .AddHttpClientInstrumentation(opts =>
                                {
                                    opts.RecordException = true;
                                })
                                .AddAspNetCoreInstrumentation(opts =>
                                {
                                    opts.RecordException = true;
                                });

                            if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
                            {
                                builder = builder.AddAzureMonitorTraceExporter(options =>
                                {
                                    options.ConnectionString = appInsightsConnectionString;
                                });
                            }
                            else
                            {
                                builder = builder.AddOtlpExporter();
                            }
                        })
                        .WithMetrics(builder =>
                        {
                            builder = builder
                                .AddMeter(appId)
                                .AddRuntimeInstrumentation()
                                .AddHttpClientInstrumentation()
                                .AddAspNetCoreInstrumentation();

                            if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
                            {
                                builder = builder.AddAzureMonitorMetricExporter(options =>
                                {
                                    options.ConnectionString = appInsightsConnectionString;
                                });
                            }
                            else
                            {
                                builder = builder.AddOtlpExporter(
                                    (_, readerOptions) =>
                                    {
                                        if (env.IsDevelopment())
                                        {
                                            // Export interval should be set by env var when running in real environments
                                            // but locally it's nice to receive metrics more often than the default 60s
                                            readerOptions
                                                .PeriodicExportingMetricReaderOptions
                                                .ExportIntervalMilliseconds = 10_000;
                                            readerOptions
                                                .PeriodicExportingMetricReaderOptions
                                                .ExportTimeoutMilliseconds = 8_000;
                                        }
                                    }
                                );
                            }
                        })
            );

            return builder;
        }

        private sealed class TelemetryInitialization(Telemetry telemetry, MeterProvider meterProvider) : IHostedService
        {
            public Task StartAsync(CancellationToken cancellationToken)
            {
                // This codepath for initialization is here only because it makes it a lot easier to
                // query the metrics from Prometheus using 'increase' without the appearance of a "missed" sample.
                // 'increase' in Prometheus will not interpret 'none' -> 1 as a delta/increase,
                // so when querying the increase within a range, there may be 1 less sample than expected.
                // So here we let the metrics be initialized to 0,
                // and then run collection/flush on the otel MeterProvider to make sure they are exported.
                // The first time we then increment the metric, it will count as a change from 0 -> 1
                telemetry.Init();
                if (!meterProvider.ForceFlush(10_000))
                {
                    throw new Exception("Couldn't initialize Telemetry service");
                }
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private static void AddAuthorizationPolicies(IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("InstanceRead", policy => policy.Requirements.Add(new AppAccessRequirement("read")));
                options.AddPolicy(
                    "InstanceWrite",
                    policy => policy.Requirements.Add(new AppAccessRequirement("write"))
                );
                options.AddPolicy(
                    "InstanceDelete",
                    policy => policy.Requirements.Add(new AppAccessRequirement("delete"))
                );
                options.AddPolicy(
                    "InstanceInstantiate",
                    policy => policy.Requirements.Add(new AppAccessRequirement("instantiate"))
                );
                options.AddPolicy(
                    "InstanceComplete",
                    policy => policy.Requirements.Add(new AppAccessRequirement("complete"))
                );
            });
        }

        private static void AddAuthenticationScheme(
            IServiceCollection services,
            IConfiguration config,
            IWebHostEnvironment env
        )
        {
            services
                .AddAuthentication(JwtCookieDefaults.AuthenticationScheme)
                .AddJwtCookie(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        RequireExpirationTime = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                    options.JwtCookieName = Altinn.App.Core.Constants.General.RuntimeCookieName;
                    options.MetadataAddress = config["AppSettings:OpenIdWellKnownEndpoint"];
                    if (env.IsDevelopment())
                    {
                        options.RequireHttpsMetadata = false;
                    }
                });
        }

        private static void AddAntiforgery(IServiceCollection services)
        {
            services.AddAntiforgery(options =>
            {
                // asp .net core expects two types of tokens: One that is attached to the request as header, and the other one as cookie.
                // The values of the tokens are not the same and both need to be present and valid in a "unsafe" request.

                // Axios which we are using for client-side automatically extracts the value from the cookie named XSRF-TOKEN. We are setting this cookie in the UserController.
                // We will therefore have two token cookies. One that contains the .net core cookie token; And one that is the request token and is added as a header in requests.
                // The tokens are based on the logged-in user and must be updated if the user changes.
                // https://docs.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-3.1
                // https://github.com/axios/axios/blob/master/lib/defaults.js
                options.Cookie.Name = "AS-XSRF-TOKEN";
                options.HeaderName = "X-XSRF-TOKEN";
            });

            services.TryAddSingleton<ValidateAntiforgeryTokenIfAuthCookieAuthorizationFilter>();
        }

        /// <summary>
        /// Get Application Insights configuration from environment variables or appsettings.json.
        /// </summary>
        /// <param name="config">config</param>
        /// <param name="env">env</param>
        /// <returns></returns>
        internal static (string? Key, string? ConnectionString) GetAppInsightsConfig(IConfiguration config, string env)
        {
            var isDevelopment = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
            string? key = isDevelopment
                ? config["ApplicationInsights:InstrumentationKey"]
                : Environment.GetEnvironmentVariable("ApplicationInsights__InstrumentationKey");
            string? connectionString = isDevelopment
                ? config["ApplicationInsights:ConnectionString"]
                : Environment.GetEnvironmentVariable("ApplicationInsights__ConnectionString");

            return (key, connectionString);
        }

        /// <summary>
        /// Stores an instance of type T in the service collection, or returns the existing instance if it already exists.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services">collection</param>
        /// <param name="factory">factory</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Invalid registration</exception>
        internal static T GetOrCreateInstanceInServices<T>(this IServiceCollection services, Func<T> factory)
            where T : class
        {
            var name = typeof(T).Name;

            var existingInstanceRegistration = services.LastOrDefault(s => s.ServiceType == typeof(T));
            T instance;
            if (existingInstanceRegistration is not null)
            {
                if (existingInstanceRegistration.ImplementationInstance is not T existingInstance)
                {
                    throw new InvalidOperationException($"{name} was already registered, but not correctly");
                }
                instance = existingInstance;
            }
            else
            {
                instance = factory();
                services.AddSingleton<T>(instance);
            }

            return instance;
        }

        /// <summary>
        /// Get an instance of type T from the service collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services">services</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Invalid registration</exception>
        internal static T GetInstanceInServices<T>(this IServiceCollection services)
            where T : class
        {
            var name = typeof(T).Name;

            var existingInstanceRegistration = services.LastOrDefault(s => s.ServiceType == typeof(T));
            if (existingInstanceRegistration is null)
            {
                throw new InvalidOperationException($"{name} was not registered");
            }

            if (existingInstanceRegistration.ImplementationInstance is not T instance)
            {
                throw new InvalidOperationException($"{name} was registered, but not correctly");
            }

            return instance;
        }
    }
}
