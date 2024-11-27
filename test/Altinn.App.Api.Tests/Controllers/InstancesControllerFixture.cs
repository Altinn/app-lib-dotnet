using Altinn.App.Api.Controllers;
using Altinn.App.Api.Helpers.Patch;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Events;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Prefill;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Internal.Validation;
using Altinn.Common.PEP.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using IProcessEngine = Altinn.App.Core.Internal.Process.IProcessEngine;

namespace Altinn.App.Api.Tests.Controllers;

internal sealed record InstancesControllerFixture(IServiceProvider ServiceProvider) : IDisposable
{
    public Mock<T> Mock<T>()
        where T : class => Moq.Mock.Get(ServiceProvider.GetRequiredService<T>());

    public void VerifyNoOtherCalls(
        bool verifyDataClient = true,
        bool verifyAppModel = true,
        bool verifyInstantiationProcessor = true,
        bool verifyPrefill = true
    )
    {
        Mock<IAltinnPartyClient>().VerifyNoOtherCalls();
        Mock<IInstanceClient>().VerifyNoOtherCalls();
        if (verifyDataClient)
            Mock<IDataClient>().VerifyNoOtherCalls();
        Mock<IAppMetadata>().VerifyNoOtherCalls();
        if (verifyAppModel)
            Mock<IAppModel>().VerifyNoOtherCalls();
        if (verifyInstantiationProcessor)
            Mock<IInstantiationProcessor>().VerifyNoOtherCalls();
        Mock<IInstantiationValidator>().VerifyNoOtherCalls();
        Mock<IPDP>().VerifyNoOtherCalls();
        Mock<IEventsClient>().VerifyNoOtherCalls();
        if (verifyPrefill)
            Mock<IPrefill>().VerifyNoOtherCalls();
        Mock<IProfileClient>().VerifyNoOtherCalls();
        Mock<IProcessEngine>().VerifyNoOtherCalls();
    }

    public void Dispose() => (ServiceProvider as IDisposable)?.Dispose();

    internal static InstancesControllerFixture Create()
    {
        var services = new ServiceCollection();
        services.AddTestAppImplementationFactory();

        Mock<ILogger<InstancesController>> logger = new();
        services.AddSingleton(logger.Object);
        Mock<IAltinnPartyClient> registrer = new();
        services.AddTransient(_ => registrer.Object);
        Mock<IInstanceClient> instanceClient = new();
        services.AddTransient(_ => instanceClient.Object);
        Mock<IDataClient> data = new();
        services.AddTransient(_ => data.Object);
        Mock<IAppMetadata> appMetadata = new();
        services.AddTransient(_ => appMetadata.Object);
        Mock<IAppModel> appModel = new();
        services.AddTransient(_ => appModel.Object);
        Mock<IInstantiationProcessor> instantiationProcessor = new();
        services.AddTransient(_ => instantiationProcessor.Object);
        Mock<IInstantiationValidator> instantiationValidator = new();
        services.AddTransient(_ => instantiationValidator.Object);
        Mock<IPDP> pdp = new();
        services.AddTransient(_ => pdp.Object);
        Mock<IEventsClient> eventsService = new();
        services.AddTransient(_ => eventsService.Object);
        services.AddOptions<AppSettings>().Configure(_ => { });
        Mock<IPrefill> prefill = new();
        services.AddTransient(_ => prefill.Object);
        Mock<IProfileClient> profile = new();
        services.AddTransient(_ => profile.Object);
        Mock<IProcessEngine> processEngine = new();
        services.AddTransient(_ => processEngine.Object);
        Mock<IOrganizationClient> oarganizationClientMock = new();
        services.AddTransient(_ => oarganizationClientMock.Object);
        Mock<IHostEnvironment> envMock = new();
        services.AddTransient(_ => envMock.Object);
        Mock<HttpContext> httpContextMock = new();
        services.AddTransient(_ => httpContextMock.Object);
        Mock<IValidationService> validationServiceMock = new();
        services.AddTransient(_ => validationServiceMock.Object);
        services.AddTransient<InternalPatchService>();
        services.AddTransient<ModelSerializationService>();

        services.AddTransient(sp =>
        {
            var controller = ActivatorUtilities.CreateInstance<InstancesController>(sp);
            controller.ControllerContext = new() { HttpContext = httpContextMock.Object };
            return controller;
        });

        var serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }
        );
        return new(serviceProvider);
    }
}
