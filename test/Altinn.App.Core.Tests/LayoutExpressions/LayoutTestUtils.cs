#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Altinn.App.Core.Configuration;
using Altinn.App.Core.Implementation.Expression;
using Altinn.App.Core.Interface;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Altinn.App.Core.Tests.LayoutExpressions;

public static class LayoutTestUtils
{
    public static async Task<LayoutModelTools> GetLayoutModelTools(object model, string folder)
    {
        var services = new ServiceCollection();

        var data = new Mock<IData>();
        data.Setup(d => d.GetFormData(default, default!, default!, default!, default, default)).ReturnsAsync(model);
        services.AddTransient<IData>((sp) => data.Object);

        var resources = new Mock<IAppResources>();
        var layouts = new Dictionary<string, object>();
        var layoutsPath = Path.Join("LayoutExpressions", "TestResources", folder);
        foreach (var layoutFile in Directory.GetFiles(layoutsPath))
        {
            string layout = await File.ReadAllTextAsync(layoutFile, Encoding.UTF8);
            string name = layoutFile.Replace(layoutsPath + "/", string.Empty).Replace(".json", string.Empty);
            layouts.Add(name, JsonSerializer.Deserialize<object>(layout)!);
        }

        resources.Setup(r => r.GetLayouts()).Returns(JsonSerializer.Serialize(layouts));

        services.AddTransient<IAppResources>((sp) => resources.Object);
        services.AddTransient<LayoutEvaluatorStateInitializer>();
        services.AddOptions<FrontEndSettings>().Configure(fes => fes.Add("test", "value"));
        services.AddTransient<LayoutModelTools>();

        var serviceProvider = services.BuildServiceProvider(validateScopes: true);

        return serviceProvider.GetRequiredService<LayoutModelTools>();
    }
}