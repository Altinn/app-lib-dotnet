// using Altinn.App.Core.Internal.AppModel;
//
// namespace Altinn.App.Integration;
//
// using Microsoft.AspNetCore.Hosting;
// using Microsoft.AspNetCore.Mvc.Testing;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using Xunit.Abstractions;
//
// public abstract class AltinnAppWebApplicationFactory<TFrontendTestMarker> : WebApplicationFactory<TFrontendTestMarker>
//     where TFrontendTestMarker : class
// {
//     protected readonly ITestOutputHelper TestOutput;
//
//     protected AltinnAppWebApplicationFactory(ITestOutputHelper testOutput)
//     {
//         TestOutput = testOutput;
//     }
//
//     protected override void ConfigureWebHost(IWebHostBuilder builder)
//     {
//         builder.ConfigureLogging(ConfigureLogging);
//         builder.ConfigureServices(ConfigureServices);
//         base.ConfigureWebHost(builder);
//     }
//
//     private void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
//     {
//         services.AddSingleton<IAppModel>(new TestAppModel<TFrontendTestMarker>());
//     }
//
//     private void ConfigureLogging(WebHostBuilderContext context, ILoggingBuilder builder)
//     {
//         builder
//             .ClearProviders()
//             .AddFakeLogging(options =>
//             {
//                 options.OutputSink = message => TestOutput.WriteLine(message);
//                 options.OutputFormatter = FakeLoggerXunit.OutputFormatter;
//             });
//     }
// }
