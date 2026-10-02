using System.Net;
using System.Text.Json;
using Altinn.App.Api.Tests.Data;
using Altinn.App.Api.Tests.Data.apps.tdd.contributer_restriction.models;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Altinn.App.Api.Tests.Controllers;

public class DataController_LayoutEvaluatorTests : ApiTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public DataController_LayoutEvaluatorTests(WebApplicationFactory<Program> factory, ITestOutputHelper outputHelper)
        : base(factory, outputHelper) { }

    private class DataProcessor : IDataProcessor
    {
        private readonly LayoutEvaluatorStateInitializer _layoutEvaluatorStateInitializer;

        public DataProcessor(LayoutEvaluatorStateInitializer layoutEvaluatorStateInitializer)
        {
            _layoutEvaluatorStateInitializer = layoutEvaluatorStateInitializer;
        }

        public Task ProcessDataRead(Instance instance, Guid? dataId, object data, string? language)
        {
            return Task.FromException(new NotImplementedException());
        }

        public async Task ProcessDataWrite(
            Instance instance,
            Guid? dataId,
            object data,
            object? previousData,
            string? language
        )
        {
            var layoutSetId = "default";
            var layoutEvaluatorState = await _layoutEvaluatorStateInitializer.Init(instance, data, layoutSetId);
            var hidden = await LayoutEvaluator.GetHiddenFieldsForRemoval(layoutEvaluatorState);
            if (dataId.HasValue)
            {
                var id = new DataElementIdentifier(dataId.Value);
                hidden
                    .Should()
                    .BeEquivalentTo([
                        new DataReference() { DataElementIdentifier = id, Field = "melding.hidden" },
                        new DataReference() { DataElementIdentifier = id, Field = "melding.hiddenNotRemove" },
                        new DataReference() { DataElementIdentifier = id, Field = "melding.hiddenPage" },
                        new DataReference() { DataElementIdentifier = id, Field = "melding.hiddenPageNotRemove" },
                    ]);
                if (data is Skjema { Melding: { } melding })
                {
                    melding.Toggle = !melding.Toggle;
                    melding.Random = dataId.ToString();
                }
            }
        }
    }

    [Fact]
    public async Task PutDataElement_LegacyLayoutEvaluatorState_ReturnsOk()
    {
        OverrideServicesForThisTest = (services) =>
        {
            services.AddSingleton<IDataProcessor, DataProcessor>();
        };
        // Setup test data
        string org = "tdd";
        string app = "contributer-restriction";
        int instanceOwnerPartyId = 500600;
        Guid instanceGuid = Guid.Parse("7f1a3c0e-4b7d-4e2a-9c61-2d8f5a6b0c13");
        Guid dataGuid = Guid.Parse("a2e9d1b4-6c3f-4d58-8e7a-1b5c9f0d2e46");
        using HttpClient client = GetRootedUserClient(org, app, 1337, instanceOwnerPartyId);

        TestData.PrepareInstance(org, app, instanceOwnerPartyId, instanceGuid);

        // Update data element
        using var updateDataElementContent = new StringContent(
            """{"melding":{"name": "Ola Nielsen"}}""",
            System.Text.Encoding.UTF8,
            "application/json"
        );
        var response = await client.PutAsync(
            $"/{org}/{app}/instances/{instanceOwnerPartyId}/{instanceGuid}/data/{dataGuid}",
            updateDataElementContent
        );
        var changes = await VerifyStatusAndDeserialize<CalculationResult>(response, HttpStatusCode.OK);
        changes.ChangedFields.Should().HaveCount(2);
        changes
            .ChangedFields.Should()
            .ContainKey("melding.toggle")
            .WhoseValue.Should()
            .BeOfType<JsonElement>()
            .Which.GetBoolean()
            .Should()
            .BeTrue();
        changes
            .ChangedFields.Should()
            .ContainKey("melding.random")
            .WhoseValue.Should()
            .BeOfType<JsonElement>()
            .Which.GetString()
            .Should()
            .Be(dataGuid.ToString());
    }
}
