using System.Text.Json;
using Altinn.App.Api.Controllers;
using Altinn.App.Api.Models;
using Altinn.App.Core.Features;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Altinn.App.Api.Tests.Controllers;

public class DataController_PatchFormDataImplementation
{
    static Guid DataGuid = new ("12345678-1234-1234-1234-123456789123");
    static Guid InstanceGuid = new ("12345678-1234-1234-1234-123456789124");
    const string Org = "ttd";
    const string App = "endring-av-navn";
    const int InstanceOwnerPartyId = 4766;
    const string InstanceOwnerId = "4766";
    private Mock<IDataProcessor> _dataProcessor = new(MockBehavior.Strict);
    private Instance instance = new ();

    public class MyModel
    {
        public string? Name { get; set; }
    }

    [Fact]
    public async Task Test()
    {
        var request = JsonSerializer.Deserialize<DataPatchRequest>("""
            {
                "patch": [
                    {
                        "op": "replace",
                        "path": "/Name",
                        "value": "Test Testesen"
                    }
                ],
                "ignoredValidators": [
                    "required"
                ]
            }
            """)!;
        var oldModel = new MyModel { Name = "OrginaltNavn" };

        _dataProcessor.Setup(d => d.ProcessDataWrite(It.IsAny<Instance>(), It.IsAny<Guid>(), It.IsAny<MyModel>())).Returns((Instance i, Guid j, MyModel data) => Task.CompletedTask);


        var response = await DataController.PatchFormDataImplementation(DataGuid, request, oldModel, instance,
            new IDataProcessor[] { _dataProcessor.Object });
        response.Should().NotBeNull();
        response.NewDataModel.Should().BeOfType<MyModel>().Subject.Name.Should().Be("Test Testesen");
    }
}