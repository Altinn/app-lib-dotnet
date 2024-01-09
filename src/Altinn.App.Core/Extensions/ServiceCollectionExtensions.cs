using Altinn.App.Api.Configuration;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Action;
using Altinn.App.Core.Features.DataLists;
using Altinn.App.Core.Features.DataProcessing;
using Altinn.App.Core.Features.FileAnalyzis;
using Altinn.App.Core.Features.Options;
using Altinn.App.Core.Features.PageOrder;
using Altinn.App.Core.Features.Pdf;
using Altinn.App.Core.Features.Validation;
using Altinn.App.Core.Features.Validation.Default;
using Altinn.App.Core.Implementation;
using Altinn.App.Core.Infrastructure.Clients.Authentication;
using Altinn.App.Core.Infrastructure.Clients.Authorization;
using Altinn.App.Core.Infrastructure.Clients.Events;
using Altinn.App.Core.Infrastructure.Clients.KeyVault;
using Altinn.App.Core.Infrastructure.Clients.Pdf;
using Altinn.App.Core.Infrastructure.Clients.Profile;
using Altinn.App.Core.Infrastructure.Clients.Register;
using Altinn.App.Core.Infrastructure.Clients.Storage;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Events;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Language;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Prefill;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Action;
using Altinn.App.Core.Internal.Process.ServiceTasks;
using Altinn.App.Core.Internal.Process.TaskTypes;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Internal.Secrets;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Internal.Texts;
using Altinn.App.Core.Models;
using Altinn.Common.AccessTokenClient.Configuration;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Common.PEP.Implementation;
using Altinn.Common.PEP.Interfaces;
using AltinnCore.Authentication.Constants;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Linq;
using Prometheus;
using IProcessEngine = Altinn.App.Core.Internal.Process.IProcessEngine;
using IProcessReader = Altinn.App.Core.Internal.Process.IProcessReader;
using ProcessEngine = Altinn.App.Core.Internal.Process.ProcessEngine;
using ProcessReader = Altinn.App.Core.Internal.Process.ProcessReader;

namespace Altinn.App.Core.Extensions
{
    /// <summary>
    /// This class holds a collection of extension methods for the <see cref="IServiceCollection"/> interface.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds all http clients for platform functionality.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> being built.</param>
        /// <param name="configuration">A reference to the current <see cref="IConfiguration"/> object.</param>
        /// <param name="env">A reference to the current <see cref="IWebHostEnvironment"/> object.</param>
        public static void AddPlatformServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
        {
            services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
            services.Configure<GeneralSettings>(configuration.GetSection("GeneralSettings"));
            services.Configure<PlatformSettings>(configuration.GetSection("PlatformSettings"));
            services.Configure<CacheSettings>(configuration.GetSection("CacheSettings"));

            AddApplicationIdentifier(services);

            services.AddHttpClient<IApplicationClient, ApplicationClient>();
            services.AddHttpClient<IAuthenticationClient, AuthenticationClient>();
            services.AddHttpClient<IAuthorizationClient, AuthorizationClient>();
            services.AddHttpClient<IDataClient, DataClient>();
            services.AddHttpClient<IOrganizationClient, RegisterERClient>();
            services.AddHttpClient<IInstanceClient, InstanceClient>();
            services.AddHttpClient<IInstanceEventClient, InstanceEventClient>();
            services.AddHttpClient<IEventsClient, EventsClient>();
            services.AddHttpClient<IProfileClient, ProfileClient>();
            services.Decorate<IProfileClient, ProfileClientCachingDecorator>();
            services.AddHttpClient<IAltinnPartyClient, AltinnPartyClient>();
#pragma warning disable CS0618 // Type or member is obsolete
            services.AddHttpClient<IText, TextClient>();
#pragma warning restore CS0618 // Type or member is obsolete
            services.AddHttpClient<IProcessClient, ProcessClient>();
            services.AddHttpClient<IPersonClient, PersonClient>();

            services.TryAddTransient<IUserTokenProvider, UserTokenProvider>();
            services.TryAddTransient<IAccessTokenGenerator, AccessTokenGenerator>();
            services.TryAddTransient<IApplicationLanguage, Internal.Language.ApplicationLanguage>();
            services.TryAddTransient<IAuthorizationService, AuthorizationService>();
        }

        private static void AddApplicationIdentifier(IServiceCollection services)
        {
            services.AddSingleton<AppIdentifier>(sp =>
            {
                var appIdentifier = GetApplicationId();
                return new AppIdentifier(appIdentifier);
            });
        }

        private static string GetApplicationId()
        {
            string appMetaDataString = File.ReadAllText("config/applicationmetadata.json");
            JObject appMetadataJObject = JObject.Parse(appMetaDataString);

            var id = appMetadataJObject?.SelectToken("id")?.Value<string>();

            if (id == null)
            {
                throw new KeyNotFoundException("Could not find id in applicationmetadata.json. Please ensure applicationmeta.json is well formed and contains a key for id.");
            }

            return id;
        }

        /// <summary>
        /// Adds all the app services.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> being built.</param>
        /// <param name="configuration">A reference to the current <see cref="IConfiguration"/> object.</param>
        /// <param name="env">A reference to the current <see cref="IWebHostEnvironment"/> object.</param>
        public static void AddAppServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
        {
            // Services for Altinn App
            services.TryAddTransient<IPDP, PDPAppSI>();
            AddValidationServices(services, configuration);
            services.TryAddTransient<IPrefill, PrefillSI>();
            services.TryAddTransient<ISigningCredentialsResolver, SigningCredentialsResolver>();
            services.TryAddSingleton<IAppResources, AppResourcesSI>();
            services.TryAddSingleton<IAppMetadata, AppMetadata>();
            services.TryAddSingleton<IFrontendFeatures, FrontendFeatures>();
            services.TryAddTransient<IAppEvents, DefaultAppEvents>();
            services.AddTransient<IProcessTaskType, DataProcessTaskType>();
            services.AddTransient<IProcessTaskType, ConfirmationProcessTaskType>();
            services.AddTransient<IProcessTaskType, FeedbackProcessTaskType>();
            services.AddTransient<IProcessTaskType, NullProcessTaskType>();
            services.AddTransient<ProcessTaskEndCommonLogic>();
            services.AddTransient<ProcessTaskStartCommonLogic>();
            services.AddTransient<ProcessTaskLockingCommonLogic>();
            services.AddTransient<PdfServiceTask>();
            services.AddTransient<EformidlingServiceTask>();
#pragma warning disable CS0618, CS0612 // Type or member is obsolete
            services.TryAddTransient<IPageOrder, DefaultPageOrder>();
#pragma warning restore CS0618, CS0612 // Type or member is obsolete
            services.TryAddTransient<IInstantiationProcessor, NullInstantiationProcessor>();
            services.TryAddTransient<IInstantiationValidator, NullInstantiationValidator>();
            services.TryAddTransient<IAppModel, DefaultAppModel>();
            services.TryAddTransient<DataListsFactory>();
            services.TryAddTransient<InstanceDataListsFactory>();
            services.TryAddTransient<IDataListsService, DataListsService>();
            services.TryAddTransient<LayoutEvaluatorStateInitializer>();
            services.Configure<Altinn.Common.PEP.Configuration.PepSettings>(configuration.GetSection("PEPSettings"));
            services.Configure<Altinn.Common.PEP.Configuration.PlatformSettings>(configuration.GetSection("PlatformSettings"));
            services.Configure<AccessTokenSettings>(configuration.GetSection("AccessTokenSettings"));
            services.Configure<FrontEndSettings>(configuration.GetSection(nameof(FrontEndSettings)));
            services.Configure<PdfGeneratorSettings>(configuration.GetSection(nameof(PdfGeneratorSettings)));
            AddAppOptions(services);
            AddActionServices(services);
            AddPdfServices(services);
            AddEventServices(services);
            AddProcessServices(services);
            AddFileAnalyserServices(services);
            AddFileValidatorServices(services);
            AddMetricsDecorators(services, configuration);

            if (!env.IsDevelopment())
            {
                services.TryAddSingleton<ISecretsClient, SecretsClient>();
                services.Configure<KeyVaultSettings>(configuration.GetSection("kvSetting"));
            }
            else
            {
                services.TryAddSingleton<ISecretsClient, SecretsLocalClient>();
            }
        }

        private static void AddValidationServices(IServiceCollection services, IConfiguration configuration)
        {
            services.TryAddTransient<IValidationService, ValidationService>();
            if (configuration.GetSection("AppSettings").Get<AppSettings>()?.RequiredValidation == true)
            {
                services.AddTransient<IFormDataValidator, RequiredLayoutValidator>();
            }

            if (configuration.GetSection("AppSettings").Get<AppSettings>()?.ExpressionValidation == true)
            {
                services.AddTransient<IFormDataValidator, ExpressionValidator>();
            }
            services.AddTransient<IFormDataValidator, DataAnnotationValidator>();
            services.AddTransient<IFormDataValidator, LegacyIInstanceValidatorFormDataValidator>();
            services.AddTransient<IDataElementValidator, DefaultDataElementValidator>();
            services.AddTransient<ITaskValidator, LegacyIInstanceValidatorTaskValidator>();
            services.AddTransient<ITaskValidator, DefaultTaskValidator>();
        }

        /// <summary>
        /// Checks if a service is already added to the collection.
        /// </summary>
        /// <returns>true if the services allready exists in the collection, otherwise false</returns>
        public static bool IsAdded(this IServiceCollection services, Type serviceType)
        {
            if (services.Any(x => x.ServiceType == serviceType))
            {
                return true;
            }

            return false;
        }

        private static void AddEventServices(IServiceCollection services)
        {
            services.AddTransient<IEventHandler, SubscriptionValidationHandler>();
            services.AddTransient<IEventHandlerResolver, EventHandlerResolver>();
            services.TryAddSingleton<IEventSecretCodeProvider, KeyVaultEventSecretCodeProvider>();

            // The event subscription client depends uppon a maskinporten messagehandler beeing
            // added to the client during setup. As of now this needs to be done in the apps
            // if subscription is to be added. This registration is to prevent the DI container
            // from failing for the apps not using event subscription. If you try to use
            // event subscription with this client you will get a 401 Unauthorized.
            if (!services.IsAdded(typeof(IEventsSubscription)))
            {
                services.AddHttpClient<IEventsSubscription, EventsSubscriptionClient>();
            }
        }

        private static void AddPdfServices(IServiceCollection services)
        {
            services.TryAddTransient<IPdfGeneratorClient, PdfGeneratorClient>();
            services.TryAddTransient<IPdfService, PdfService>();
#pragma warning disable CS0618 // Type or member is obsolete
            services.TryAddTransient<IPdfFormatter, NullPdfFormatter>();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private static void AddAppOptions(IServiceCollection services)
        {
            // Main service for interacting with options
            services.TryAddTransient<IAppOptionsService, AppOptionsService>();

            // Services related to application options
            services.TryAddTransient<AppOptionsFactory>();
            services.AddTransient<IAppOptionsProvider, DefaultAppOptionsProvider>();
            services.TryAddTransient<IAppOptionsFileHandler, AppOptionsFileHandler>();

            // Services related to instance aware and secure app options
            services.TryAddTransient<InstanceAppOptionsFactory>();
        }

        private static void AddProcessServices(IServiceCollection services)
        {
            services.TryAddTransient<IProcessEngine, ProcessEngine>();
            services.TryAddTransient<IProcessNavigator, ProcessNavigator>();
            services.TryAddSingleton<IProcessReader, ProcessReader>();
            services.TryAddTransient<IProcessEventDispatcher, ProcessEventDispatcher>();
            services.AddTransient<IProcessExclusiveGateway, ExpressionsExclusiveGateway>();
            services.TryAddTransient<ExclusiveGatewayFactory>();
        }

        private static void AddActionServices(IServiceCollection services)
        {
            services.TryAddTransient<UserActionService>();
            services.AddTransient<IUserAction, SigningUserAction>();
            services.AddHttpClient<ISignClient, SignClient>();
            services.AddTransientUserActionAuthorizerForActionInAllTasks<UniqueSignatureAuthorizer>("sign");
        }

        private static void AddFileAnalyserServices(IServiceCollection services)
        {
            services.TryAddTransient<IFileAnalysisService, FileAnalysisService>();
            services.TryAddTransient<IFileAnalyserFactory, FileAnalyserFactory>();
        }

        private static void AddFileValidatorServices(IServiceCollection services)
        {
            services.TryAddTransient<IFileValidationService, FileValidationService>();
            services.TryAddTransient<IFileValidatorFactory, FileValidatorFactory>();
        }

        private static void AddMetricsDecorators(IServiceCollection services, IConfiguration configuration)
        {
            MetricsSettings metricsSettings = configuration.GetSection("MetricsSettings")?.Get<MetricsSettings>() ?? new MetricsSettings();
            if (metricsSettings.Enabled)
            {
                services.Decorate<IInstanceClient, InstanceClientMetricsDecorator>();
                services.Decorate<IProcessEngine, ProcessEngineMetricsDecorator>();
            }
        }
    }
}
