using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.App.Core.Models.Layout.Components;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Altinn.App.Core.Tests.LayoutExpressions;

public static class LayoutTestUtils
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public const string Org = "ttd";
    public const string App = "test";
    public const string AppId = $"{Org}/{App}";
    public const int InstanceOwnerPartyId = 134;
    public static Guid InstanceGuid = Guid.Parse("12345678-1234-1234-1234-123456789012");
    public static Guid DataGuid = Guid.Parse("12345678-1234-1234-1234-123456789013");
    public const string DataTypeId = "default";
    public const string ClassRef = "NoClass";
    public const string TaskId = "Task_1";

    public static ApplicationMetadata ApplicationMetadata =
        new(AppId)
        {
            DataTypes = new List<DataType>()
            {
                new()
                {
                    Id = DataTypeId,
                    TaskId = TaskId,
                    AppLogic = new() { ClassRef = ClassRef }
                }
            }
        };

    public static Instance Instance =
        new()
        {
            Id = $"{InstanceOwnerPartyId}/{InstanceGuid}",
            AppId = AppId,
            Org = Org,
            InstanceOwner = new() { PartyId = InstanceOwnerPartyId.ToString() },
            Data = new()
            {
                new() { Id = DataGuid.ToString(), DataType = "default", }
            }
        };

    public static async Task<LayoutEvaluatorState> GetLayoutModelTools(object model, string folder)
    {
        var services = new ServiceCollection();

        var appMetadata = new Mock<IAppMetadata>(MockBehavior.Strict);

        appMetadata.Setup(am => am.GetApplicationMetadata()).ReturnsAsync(ApplicationMetadata);
        var appModel = new Mock<IAppModel>(MockBehavior.Strict);
        var modelType = model.GetType();
        appModel.Setup(am => am.GetModelType(ClassRef)).Returns(modelType);

        var data = new Mock<IDataClient>(MockBehavior.Strict);
        data.Setup(d => d.GetFormData(InstanceGuid, modelType, Org, App, InstanceOwnerPartyId, DataGuid))
            .ReturnsAsync(model);
        services.AddTransient<IDataClient>((sp) => data.Object);

        var resources = new Mock<IAppResources>();
        var pages = new Dictionary<string, PageComponent>();
        var layoutsPath = Path.Join("LayoutExpressions", "FullTests", folder);
        foreach (var layoutFile in Directory.GetFiles(layoutsPath, "*.json"))
        {
            var layout = await File.ReadAllBytesAsync(layoutFile);
            string pageName = layoutFile.Replace(layoutsPath + "/", string.Empty).Replace(".json", string.Empty);

            PageComponentConverter.SetAsyncLocalPageName(pageName);

            pages[pageName] = JsonSerializer.Deserialize<PageComponent>(layout.RemoveBom(), _jsonSerializerOptions)!;
        }
        var layoutModel = new LayoutModel()
        {
            DefaultDataType = new DataType() { Id = DataTypeId, },
            Pages = pages
        };

        resources.Setup(r => r.GetLayoutModelForTask(TaskId)).Returns(layoutModel);

        services.AddSingleton(resources.Object);
        services.AddSingleton(appMetadata.Object);
        services.AddSingleton(appModel.Object);
        services.AddScoped<ILayoutEvaluatorStateInitializer, LayoutEvaluatorStateInitializer>();
        services.AddScoped<ICachedFormDataAccessor, CachedFormDataAccessor>();
        services.AddOptions<FrontEndSettings>().Configure(fes => fes.Add("test", "value"));

        var serviceProvider = services.BuildServiceProvider(validateScopes: true);
        using var scope = serviceProvider.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<ILayoutEvaluatorStateInitializer>();

        return await initializer.Init(Instance, TaskId);
    }
}
