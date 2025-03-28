using System.Runtime.CompilerServices;
using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.App.Core.Models.Layout.Components;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.App.Core.Tests.LayoutExpressions.FullTests;

public sealed class DataAccessorFixture
{
    public const string Org = "ttd";
    public const string App = "data-accessor-fixture";
    public const string TaskId = "Task_1-access";
    public const int InstanceOwnerPartyId = 1337;
    public static readonly Guid InstanceGuid = Guid.Parse("00000000-BABE-0000-0000-000000000001");

    public Mock<IAppResources> AppResourcesMock { get; } = new(MockBehavior.Strict);
    public Mock<IAppMetadata> AppMetadataMock { get; } = new(MockBehavior.Strict);
    public Mock<IAppModel> AppModelMock { get; } = new(MockBehavior.Strict);

    public Mock<IDataClient> DataClientMock { get; } = new(MockBehavior.Strict);
    public Mock<IInstanceClient> InstanceClientMock { get; } = new(MockBehavior.Strict);

    public FrontEndSettings FrontEndSettings { get; } = new();
    public ApplicationMetadata ApplicationMetadata { get; } = new($"{Org}/{App}") { DataTypes = [] };

    public Instance Instance = new()
    {
        Id = $"{InstanceOwnerPartyId}/{InstanceGuid}",
        InstanceOwner = new() { PartyId = InstanceOwnerPartyId.ToString() },
        Data = [],
    };

    private readonly IServiceCollection _serviceCollection = new ServiceCollection();

    public ServiceProvider BuildServiceProvider() => _serviceCollection.BuildServiceProvider();

    private DataAccessorFixture()
    {
        AppMetadataMock.Setup(a => a.GetApplicationMetadata()).ReturnsAsync(ApplicationMetadata);
        _serviceCollection.AddSingleton(AppResourcesMock.Object);
        _serviceCollection.AddSingleton(AppMetadataMock.Object);
        _serviceCollection.AddSingleton(Options.Create(FrontEndSettings));
        _serviceCollection.AddSingleton(AppModelMock.Object);
        _serviceCollection.AddSingleton(DataClientMock.Object);
        _serviceCollection.AddSingleton(InstanceClientMock.Object);
        _serviceCollection.AddSingleton<InstanceDataUnitOfWorkInitializer>();
        _serviceCollection.AddSingleton<ModelSerializationService>();
    }

    public static async Task<DataAccessorFixture> CreateAsync(
        List<LayoutSetSpec> specs,
        [CallerFilePath] string callerFilePath = ""
    )
    {
        var fixture = new DataAccessorFixture();
        await fixture.AddLayouts(specs, callerFilePath);
        return fixture;
    }

    public record LayoutSetSpec(string LayoutSetName, Type Type, int MaxCount);

    /// <summary>
    /// The first spec is the default layout set. The remaining can be referenced as subforms
    /// </summary>
    private async Task AddLayouts(List<LayoutSetSpec> specs, string callerFilePath)
    {
        var directory =
            Path.GetDirectoryName(callerFilePath) ?? throw new InvalidOperationException("Could not get directory");
        List<LayoutSetComponent> layouts = [];

        foreach (var spec in specs)
        {
            var pageNames = Directory
                .GetFiles(Path.Join(directory, spec.LayoutSetName), "*.json")
                .Select(Path.GetFileNameWithoutExtension);

            var pages = await Task.WhenAll(
                pageNames.Select(async pageName =>
                {
                    var pageText = await File.ReadAllTextAsync(
                        Path.Join(directory, spec.LayoutSetName, $"{pageName}.json")
                    );
                    PageComponentConverter.SetAsyncLocalPageName(spec.LayoutSetName, pageName!);
                    var pageComponent = JsonSerializer.Deserialize<PageComponent>(pageText)!;
                    return pageComponent;
                })
            );

            var dataType = new DataType()
            {
                Id = spec.LayoutSetName + "_dataType",
                TaskId = TaskId,
                AppLogic = new() { ClassRef = spec.Type.FullName },
                MaxCount = spec.MaxCount,
            };
            ApplicationMetadata.DataTypes.Add(dataType);

            AppModelMock.Setup(am => am.GetModelType(spec.Type.FullName!)).Returns(spec.Type);
            AppModelMock.Setup(am => am.Create(spec.Type.FullName!)).Returns(Activator.CreateInstance(spec.Type)!);

            var layoutSet = new LayoutSetComponent(pages.ToList(), spec.LayoutSetName, dataType);
            layouts.Add(layoutSet);
        }

        var layoutModel = new LayoutModel(layouts, null);
        AppResourcesMock.Setup(ar => ar.GetLayoutModelForTask(TaskId)).Returns(layoutModel);
    }

    public void AddFormData(object data)
    {
        var fullName = data.GetType().FullName;
        var dataType = ApplicationMetadata.DataTypes.Find(dt => dt.AppLogic?.ClassRef == fullName);
        if (dataType == null)
        {
            dataType = new DataType()
            {
                Id = data.GetType().Name,
                TaskId = TaskId,
                AppLogic = new() { ClassRef = fullName },
            };
            ApplicationMetadata.DataTypes.Add(dataType);
            AppModelMock.Setup(am => am.GetModelType(fullName!)).Returns(data.GetType());
            AppModelMock.Setup(am => am.Create(fullName!)).Returns(Activator.CreateInstance(data.GetType())!);
        }
        var dataGuid = Guid.NewGuid();
        var dataElement = new DataElement() { Id = dataGuid.ToString(), DataType = dataType.Id };
        Instance.Data.Add(dataElement);
        var serializationService = new ModelSerializationService(AppModelMock.Object);
        DataClientMock
            .Setup(dc => dc.GetDataBytes(Org, App, InstanceOwnerPartyId, InstanceGuid, dataGuid))
            .ReturnsAsync(serializationService.SerializeToStorage(data, dataType).data.ToArray());
    }
}
