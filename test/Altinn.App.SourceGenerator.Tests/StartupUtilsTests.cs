using Xunit;
using Altinn.App.Generated.Startup;
using FluentAssertions;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit.Abstractions;

namespace Altinn.App.SourceGenerator.Tests;

public class StartupUtilsTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public StartupUtilsTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void GetApplicationId_returns_id_from_applicaitonmetadata()
    {
        var appId = StartupUtils.GetApplicationId();
        appId.Should().Be("xunit/test-app");
    }

    [Fact]
    public void IncludeXmlComments_calls_IncludeXmlComments()
    {
        var paramsFilterDesc = new List<FilterDescriptor>();
        var reqBodyFilters = new List<FilterDescriptor>();
        var opFilter = new List<FilterDescriptor>();
        var schemaFilter = new List<FilterDescriptor>();
        var swaggerOptions = new SwaggerGenOptions()
        {
            ParameterFilterDescriptors = paramsFilterDesc,
            RequestBodyFilterDescriptors = reqBodyFilters,
            OperationFilterDescriptors = opFilter,
            SchemaFilterDescriptors = schemaFilter
        };
        StartupUtils.IncludeXmlComments(swaggerOptions);
        paramsFilterDesc.Should().HaveCount(2);
        reqBodyFilters.Should().HaveCount(2);
        opFilter.Should().HaveCount(2);
        schemaFilter.Should().HaveCount(2);
    }
    
}