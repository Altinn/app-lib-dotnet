using System.Collections;
using Altinn.App.Api.Controllers;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Validation;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Altinn.App.Api.Tests.Controllers;

public class TestScenariosData : IEnumerable<object[]>
{
    // Add new test data in this list
    private readonly List<ValidateDataTestScenario> _data = new List<ValidateDataTestScenario>
    {
        new("returns_NotFound_when_GetInstance_returns_null")
        {
            ReceivedInstance = null,
            ExpectedResult = typeof(NotFoundResult)
        },
        new("thows_ValidationException_when_instance_process_is_null")
        {
            ReceivedInstance = new Instance { Process = null },
            ExpectedExceptionMessage = "Unable to validate instance without a started process."
        },
        new("thows_ValidationException_when_Instance_Process_CurrentTask_is_null")
        {
            ReceivedInstance = new Instance { Process = new ProcessState { CurrentTask = null } },
            ExpectedExceptionMessage = "Unable to validate instance without a started process."
        },
        new("thows_ValidationException_when_Instance_Data_is_empty")
        {
            ReceivedInstance = new Instance
            {
                Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "1234" } },
                Data = new List<DataElement>()
            },
            ExpectedExceptionMessage = "Unable to validate data element."
        },
        new("thows_ValidationException_when_Application_DataTypes_is_empty")
        {
            DataGuid = Guid.ParseExact("0fc98a23-fe31-4ef5-8fb9-dd3f479354cd", "D"),
            ReceivedInstance = new Instance
            {
                Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "1234" } },
                Data = new List<DataElement> { new DataElement { Id = "0fc98a23-fe31-4ef5-8fb9-dd3f479354cd" } }
            },
            ReceivedApplication = new ApplicationMetadata("ttd/test") { DataTypes = new List<DataType>() },
            ExpectedExceptionMessage = "Unknown element type."
        },
        new("adds_ValidationIssue_when_DataType_TaskId_does_not_match_CurrentTask_ElementId")
        {
            InstanceId = Guid.ParseExact("0fc98a23-fe31-4ef5-8fb9-dd3f479354ef", "D"),
            DataGuid = Guid.ParseExact("0fc98a23-fe31-4ef5-8fb9-dd3f479354cd", "D"),
            ReceivedInstance = new Instance
            {
                Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = "1234" } },
                Data = new List<DataElement>
                {
                    new DataElement
                    {
                        Id = "0fc98a23-fe31-4ef5-8fb9-dd3f479354cd",
                        DataType = "0fc98a23-fe31-4ef5-8fb9-dd3f479354cd"
                    }
                }
            },
            ReceivedApplication = new ApplicationMetadata("ttd/test")
            {
                DataTypes = new List<DataType>
                {
                    new DataType { Id = "0fc98a23-fe31-4ef5-8fb9-dd3f479354cd", TaskId = "1234" }
                }
            },
            ReceivedValidationIssues = new List<ValidationIssueWithSource>(),
            ExpectedValidationIssues = new List<ValidationIssueWithSource>
            {
                new(
                    new ValidationIssue
                    {
                        Code = ValidationIssueCodes.DataElementCodes.DataElementValidatedAtWrongTask,
                        Severity = ValidationIssueSeverity.Warning,
                        DataElementId = "0fc98a23-fe31-4ef5-8fb9-dd3f479354cd",
                        Description = AppTextHelper.GetAppText(
                            ValidationIssueCodes.DataElementCodes.DataElementValidatedAtWrongTask,
                            new Dictionary<string, Dictionary<string, string>>(),
                            null,
                            "nb"
                        )
                    },
                    "source"
                )
            },
            ExpectedResult = typeof(OkObjectResult)
        },
        new("returns_ValidationIssues_from_ValidationService")
        {
            InstanceId = Guid.ParseExact("0fc98a23-fe31-4ef5-8fb9-dd3f479354ef", "D"),
            DataGuid = Guid.ParseExact("0fc98a23-fe31-4ef5-8fb9-dd3f479354cd", "D"),
            ReceivedInstance = new Instance
            {
                AppId = "ttd/test",
                Org = "ttd",
                Process = new ProcessState
                {
                    CurrentTask = new ProcessElementInfo { ElementId = "0fc98a23-fe31-4ef5-8fb9-dd3f479354cd" }
                },
                Data = new List<DataElement>
                {
                    new DataElement
                    {
                        Id = "0fc98a23-fe31-4ef5-8fb9-dd3f479354cd",
                        DataType = "0fc98a23-fe31-4ef5-8fb9-dd3f479354cd"
                    }
                }
            },
            ReceivedApplication = new ApplicationMetadata("ttd/test")
            {
                DataTypes = new List<DataType>
                {
                    new DataType { Id = "0fc98a23-fe31-4ef5-8fb9-dd3f479354cd", TaskId = "1234" }
                }
            },
            ReceivedValidationIssues = new List<ValidationIssueWithSource>
            {
                new ValidationIssueWithSource(
                    new()
                    {
                        Code = ValidationIssueCodes.DataElementCodes.DataElementTooLarge,
                        Severity = ValidationIssueSeverity.Fixed
                    },
                    "source"
                )
            },
            ExpectedValidationIssues = new List<ValidationIssueWithSource>
            {
                new ValidationIssueWithSource(
                    new()
                    {
                        Code = ValidationIssueCodes.DataElementCodes.DataElementTooLarge,
                        Severity = ValidationIssueSeverity.Fixed
                    },
                    "source"
                )
            },
            ExpectedResult = typeof(OkObjectResult)
        }
    };

    public IEnumerator<object[]> GetEnumerator()
    {
        List<object[]> testData = new List<object[]>();
        foreach (var d in _data)
        {
            testData.Add([d]);
        }

        return testData.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class ValidationControllerValidateDataTests
{
    private readonly Mock<IInstanceClient> _instanceMock = new();
    private readonly Mock<IAppMetadata> _appMetadataMock = new();
    private readonly Mock<IValidationService> _validationMock = new();
    private readonly Mock<IDataClient> _dataClientMock = new();
    private readonly Mock<IAppModel> _appModelMock = new();

    [Theory]
    [ClassData(typeof(TestScenariosData))]
    public async Task TestValidateData(ValidateDataTestScenario testScenario)
    {
        // Arrange
        const string org = "ttd";
        const string app = "app-test";
        const int instanceOwnerId = 1337;

        SetupMocks(app, org, instanceOwnerId, testScenario);
        var validateController = new ValidateController(
            _instanceMock.Object,
            _validationMock.Object,
            _appMetadataMock.Object,
            _dataClientMock.Object,
            _appModelMock.Object
        );
        ;

        // Act and Assert
        if (testScenario.ExpectedExceptionMessage == null)
        {
            var result = await validateController.ValidateData(
                org,
                app,
                instanceOwnerId,
                testScenario.InstanceId,
                testScenario.DataGuid
            );
            result.Should().BeOfType(testScenario.ExpectedResult);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () =>
                    validateController.ValidateData(
                        org,
                        app,
                        instanceOwnerId,
                        testScenario.InstanceId,
                        testScenario.DataGuid
                    )
            );
            Assert.Equal(testScenario.ExpectedExceptionMessage, exception.Message);
        }
    }

    private void SetupMocks(string app, string org, int instanceOwnerId, ValidateDataTestScenario testScenario)
    {
        if (testScenario.ReceivedInstance != null)
        {
            _instanceMock
                .Setup(i => i.GetInstance(app, org, instanceOwnerId, testScenario.InstanceId))
                .Returns(Task.FromResult<Instance>(testScenario.ReceivedInstance));
        }
        if (testScenario.ReceivedApplication != null)
        {
            _appMetadataMock.Setup(a => a.GetApplicationMetadata()).ReturnsAsync(testScenario.ReceivedApplication);
        }

        if (
            testScenario.ReceivedInstance != null
            && testScenario.ReceivedApplication != null
            && testScenario.ReceivedValidationIssues != null
        )
        {
            _validationMock
                .Setup(v =>
                    v.ValidateInstanceAtTask(
                        testScenario.ReceivedInstance,
                        "Task_1",
                        It.IsAny<IInstanceDataAccessor>(),
                        null
                    )
                )
                .ReturnsAsync(testScenario.ReceivedValidationIssues);
        }
    }
}

public class ValidateDataTestScenario
{
    public ValidateDataTestScenario(string testScenarioName)
    {
        TestScenarioName = testScenarioName;
    }

    public string TestScenarioName { get; init; }
    public Guid InstanceId { get; init; } = Guid.NewGuid();
    public Guid DataGuid { get; init; } = Guid.NewGuid();
    public Instance? ReceivedInstance { get; init; }
    public ApplicationMetadata? ReceivedApplication { get; init; }
    public List<ValidationIssueWithSource>? ReceivedValidationIssues { get; init; }
    public string? ExpectedExceptionMessage { get; init; }
    public Type? ExpectedResult { get; init; }
    public List<ValidationIssueWithSource>? ExpectedValidationIssues { get; init; }

    public override string ToString()
    {
        return TestScenarioName;
    }
}
