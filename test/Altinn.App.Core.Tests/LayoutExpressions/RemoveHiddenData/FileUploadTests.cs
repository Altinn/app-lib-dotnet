using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Expressions;
using Altinn.App.Core.Internal.Process.ProcessTasks;
using Altinn.App.Core.Internal.Texts;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout.Components;
using Altinn.App.Tests.Common.Fixtures;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Altinn.App.Core.Tests.LayoutExpressions.RemoveHiddenData;

public class FileUploadTests
{
    private const string TaskId = "Task_1";
    private const int PartyId = 123456;

    private readonly MockedServiceCollection _collection;
    private readonly DataType _dataType;

    public class SkjemaModel
    {
        [JsonPropertyName("hasDispensation")]
        public bool HasDispensation { get; set; }

        [JsonPropertyName("hideContract")]
        public bool HideContract { get; set; }

        [JsonPropertyName("contractId")]
        public string? ContractId { get; set; }

        [JsonPropertyName("hidePage2")]
        public bool HidePage2 { get; set; }

        [JsonPropertyName("rows")]
        public List<Row>? Rows { get; set; }
    }

    public class Row
    {
        [JsonPropertyName("hideRow")]
        public bool HideRow { get; set; }

        [JsonPropertyName("hideUpload")]
        public bool HideUpload { get; set; }

        [JsonPropertyName("attachmentIds")]
        public List<string>? AttachmentIds { get; set; }
    }

    public FileUploadTests(ITestOutputHelper outputHelper)
    {
        _collection = new MockedServiceCollection { OutputHelper = outputHelper };
        _dataType = _collection.AddDataType<SkjemaModel>();
        foreach (
            var id in new[]
            {
                "dispensation",
                "alwaysShown",
                "keptWhenHidden",
                "contract",
                "rowUpload",
                "unboundRowUpload",
                "page2Upload",
            }
        )
        {
            _collection.AddDataType(new DataType { Id = id, TaskId = TaskId });
        }
        _collection.AddDataType(new DataType { Id = "otherTask", TaskId = "Task_2" });

        var layoutId = $"layoutSet-{TaskId}";
        _collection.AddLayoutSet(
            _dataType,
            [
                ParsePage(
                    "page1",
                    layoutId,
                    $$"""
                    {
                        "data": {
                            "layout": [
                                {
                                    "id": "dispensation",
                                    "type": "FileUpload",
                                    "hidden": ["equals", ["dataModel", "hasDispensation"], false]
                                },
                                {
                                    "id": "alwaysShown",
                                    "type": "FileUpload"
                                },
                                {
                                    "id": "keptWhenHidden",
                                    "type": "FileUpload",
                                    "hidden": true,
                                    "removeWhenHidden": false
                                },
                                {
                                    "id": "otherTask",
                                    "type": "FileUpload",
                                    "hidden": true
                                },
                                {
                                    "id": "withoutDataType",
                                    "type": "FileUpload",
                                    "hidden": true
                                },
                                {
                                    "id": "{{_dataType.Id}}",
                                    "type": "FileUpload",
                                    "hidden": true
                                },
                                {
                                    "id": "contract",
                                    "type": "FileUploadWithTag",
                                    "dataModelBindings": {
                                        "simpleBinding": "contractId"
                                    },
                                    "hidden": ["dataModel", "hideContract"]
                                },
                                {
                                    "id": "rows",
                                    "type": "RepeatingGroup",
                                    "dataModelBindings": {
                                        "group": "rows"
                                    },
                                    "hiddenRow": ["dataModel", "rows.hideRow"],
                                    "children": ["rowUpload", "unboundRowUpload"]
                                },
                                {
                                    "id": "rowUpload",
                                    "type": "FileUpload",
                                    "dataModelBindings": {
                                        "list": "rows.attachmentIds"
                                    },
                                    "hidden": ["dataModel", "rows.hideUpload"]
                                },
                                {
                                    "id": "unboundRowUpload",
                                    "type": "FileUpload"
                                }
                            ]
                        }
                    }
                    """
                ),
                ParsePage(
                    "page2",
                    layoutId,
                    """
                    {
                        "data": {
                            "hidden": ["dataModel", "hidePage2"],
                            "layout": [
                                {
                                    "id": "page2Upload",
                                    "type": "FileUpload"
                                }
                            ]
                        }
                    }
                    """
                ),
            ]
        );
    }

    private static PageComponent ParsePage(string pageId, string layoutId, string pageJson)
    {
        using var document = JsonDocument.Parse(pageJson);
        return PageComponent.Parse(document.RootElement, pageId, layoutId);
    }

    private static DataElement NewAttachment(string dataType) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            DataType = dataType,
            ContentType = "application/pdf",
        };

    /// <summary>
    /// Create an instance with the form data and the attachments in storage, and return a copy of it as the app reads it
    /// </summary>
    private Instance CreateInstance(WrappedServiceProvider provider, SkjemaModel model, DataElement[] attachments)
    {
        var formData = new DataElement
        {
            Id = Guid.NewGuid().ToString(),
            DataType = _dataType.Id,
            ContentType = "application/xml",
        };
        var instance = new Instance
        {
            Id = $"{PartyId}/{Guid.NewGuid()}",
            AppId = $"{MockedServiceCollection.Org}/{MockedServiceCollection.App}",
            InstanceOwner = new InstanceOwner { PartyId = PartyId.ToString() },
            Data = [formData, .. attachments],
            Process = new ProcessState { CurrentTask = new ProcessElementInfo { ElementId = TaskId } },
        };

        var serializer = provider.GetRequiredService<ModelSerializationService>();
        _collection.Storage.AddInstance(instance);
        _collection.Storage.AddDataRaw(
            Guid.Parse(formData.Id),
            serializer.SerializeToStorage(model, _dataType, formData).data.ToArray()
        );
        foreach (var attachment in attachments)
        {
            _collection.Storage.AddDataRaw(Guid.Parse(attachment.Id), [0x25, 0x50, 0x44, 0x46]);
        }

        return JsonSerializer.Deserialize<Instance>(JsonSerializer.SerializeToUtf8Bytes(instance))!;
    }

    private async Task<List<string>> GetHiddenAttachmentIds(SkjemaModel model, params DataElement[] attachments)
    {
        await using var provider = _collection.BuildServiceProvider();
        var instance = CreateInstance(provider, model, attachments);
        var dataMutator = await provider
            .GetRequiredService<InstanceDataUnitOfWorkInitializer>()
            .Init(instance, TaskId, null);

        var state = dataMutator.GetLayoutEvaluatorState();
        var hiddenAttachments = await LayoutEvaluator.GetHiddenAttachmentsForRemoval(state, TaskId);
        return hiddenAttachments.Select(a => a.Id).Order().ToList();
    }

    private static List<string> SortedIds(params DataElement[] dataElements) =>
        dataElements.Select(d => d.Id).Order().ToList();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Upload_ReturnsAllItsAttachments_OnlyWhenHidden(bool hasDispensation)
    {
        var dispensation1 = NewAttachment("dispensation");
        var dispensation2 = NewAttachment("dispensation");
        var alwaysShown = NewAttachment("alwaysShown");

        var actual = await GetHiddenAttachmentIds(
            new SkjemaModel { HasDispensation = hasDispensation },
            dispensation1,
            dispensation2,
            alwaysShown
        );

        Assert.Equal(hasDispensation ? [] : SortedIds(dispensation1, dispensation2), actual);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UploadOnPage_ReturnsItsAttachments_OnlyWhenThePageIsHidden(bool hidePage2)
    {
        var page2Upload = NewAttachment("page2Upload");

        var actual = await GetHiddenAttachmentIds(
            new SkjemaModel { HasDispensation = true, HidePage2 = hidePage2 },
            page2Upload
        );

        Assert.Equal(hidePage2 ? SortedIds(page2Upload) : [], actual);
    }

    [Fact]
    public async Task UploadInRepeatingGroup_ReturnsTheAttachmentsInHiddenRowsAndHiddenUploads()
    {
        var visible = NewAttachment("rowUpload");
        var uploadHidden = NewAttachment("rowUpload");
        var rowHidden = NewAttachment("rowUpload");
        var notInAnyRow = NewAttachment("rowUpload");

        var actual = await GetHiddenAttachmentIds(
            new SkjemaModel
            {
                HasDispensation = true,
                Rows =
                [
                    new Row { AttachmentIds = [visible.Id] },
                    new Row { HideUpload = true, AttachmentIds = [uploadHidden.Id] },
                    new Row { HideRow = true, AttachmentIds = [rowHidden.Id] },
                ],
            },
            visible,
            uploadHidden,
            rowHidden,
            notInAnyRow
        );

        // No row has notInAnyRow in its binding, so no component owns it
        Assert.Equal(SortedIds(uploadHidden, rowHidden), actual);
    }

    [Fact]
    public async Task UploadWithoutBindingInRepeatingGroup_ReturnsNothing_EvenInAHiddenRow()
    {
        // As in app-frontend, an upload in a repeating group needs a binding value to own an attachment
        var actual = await GetHiddenAttachmentIds(
            new SkjemaModel { HasDispensation = true, Rows = [new Row { HideRow = true }] },
            NewAttachment("unboundRowUpload")
        );

        Assert.Empty(actual);
    }

    [Theory]
    [InlineData("bound")]
    [InlineData(null)]
    [InlineData("")]
    public async Task HiddenUploadWithSimpleBinding_ReturnsTheBoundAttachment_OrAllWithoutBindingValue(
        string? contractId
    )
    {
        var contract = NewAttachment("contract");
        var otherContract = NewAttachment("contract");
        bool bound = contractId == "bound";

        var actual = await GetHiddenAttachmentIds(
            new SkjemaModel
            {
                HasDispensation = true,
                HideContract = true,
                ContractId = bound ? contract.Id : contractId,
            },
            contract,
            otherContract
        );

        Assert.Equal(bound ? SortedIds(contract) : SortedIds(contract, otherContract), actual);
    }

    [Fact]
    public async Task HiddenUpload_WithRemoveWhenHiddenFalseOrDataTypeInAnotherTask_ReturnsNothing()
    {
        var actual = await GetHiddenAttachmentIds(
            new SkjemaModel { HasDispensation = true },
            NewAttachment("keptWhenHidden"),
            NewAttachment("otherTask")
        );

        Assert.Empty(actual);
    }

    [Fact]
    public async Task HiddenUpload_WithoutDataTypeOrWithTheFormDataTypeId_ReturnsNothing()
    {
        // "withoutDataType" has no data type, and the upload with the form data type's id must not return the form data
        var actual = await GetHiddenAttachmentIds(new SkjemaModel { HasDispensation = true });

        Assert.Empty(actual);
    }

    [Fact]
    public async Task TaskWithoutLayouts_ReturnsNothing()
    {
        await using var provider = _collection.BuildServiceProvider();
        var instance = CreateInstance(provider, new SkjemaModel(), [NewAttachment("dispensation")]);
        var dataMutator = await provider
            .GetRequiredService<InstanceDataUnitOfWorkInitializer>()
            .Init(instance, TaskId, null);
        var state = new LayoutEvaluatorState(
            dataMutator,
            componentModel: null,
            provider.GetRequiredService<ITranslationService>(),
            new FrontEndSettings()
        );

        Assert.Empty(await LayoutEvaluator.GetHiddenAttachmentsForRemoval(state, TaskId));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProcessTaskFinalizer_DeletesAttachmentsInHiddenUploads_OnlyWithRemoveHiddenData(
        bool removeHiddenData
    )
    {
        _collection.AppSettings.RemoveHiddenData = removeHiddenData;
        RegisterFinalizer(userCanDelete: true);
        var dispensation = NewAttachment("dispensation");
        var alwaysShown = NewAttachment("alwaysShown");
        var rowHidden = NewAttachment("rowUpload");

        await using var provider = _collection.BuildServiceProvider();
        var model = new SkjemaModel
        {
            HasDispensation = false,
            Rows = [new Row { HideRow = true, AttachmentIds = [rowHidden.Id] }],
        };
        var instance = CreateInstance(provider, model, [dispensation, alwaysShown, rowHidden]);

        await provider.GetRequiredService<IProcessTaskFinalizer>().Finalize(TaskId, instance);

        var expectedDeleted = removeHiddenData ? SortedIds(dispensation, rowHidden) : [];
        var deleted = _collection
            .Storage.RequestsResponses.Where(r => r.RequestMethod == HttpMethod.Delete)
            .Select(r => r.RequestUrl!.AbsolutePath.Split('/').Last())
            .Order()
            .ToList();
        Assert.Equal(expectedDeleted, deleted);

        var stored = _collection.Storage.GetInstanceAndData(PartyId, Guid.Parse(instance.Id.Split('/')[1])).instance;
        foreach (var attachment in new[] { dispensation, alwaysShown, rowHidden })
        {
            bool shouldExist = !expectedDeleted.Contains(attachment.Id);
            Assert.Equal(shouldExist, instance.Data.Exists(d => d.Id == attachment.Id));
            Assert.Equal(shouldExist, stored.Data.Exists(d => d.Id == attachment.Id));
        }
    }

    [Fact]
    public async Task ProcessTaskFinalizer_KeepsAttachmentsTheUserCannotDelete()
    {
        _collection.AppSettings.RemoveHiddenData = true;
        RegisterFinalizer(userCanDelete: false);
        var dispensation = NewAttachment("dispensation");

        await using var provider = _collection.BuildServiceProvider();
        var instance = CreateInstance(provider, new SkjemaModel { HasDispensation = false }, [dispensation]);

        await provider.GetRequiredService<IProcessTaskFinalizer>().Finalize(TaskId, instance);

        Assert.DoesNotContain(_collection.Storage.RequestsResponses, r => r.RequestMethod == HttpMethod.Delete);
        Assert.Contains(instance.Data, d => d.Id == dispensation.Id);
    }

    private void RegisterFinalizer(bool userCanDelete)
    {
        _collection.Services.AddTransient<IProcessTaskFinalizer, ProcessTaskFinalizer>();
        _collection.Services.AddTransient<ILayoutEvaluatorStateInitializer, LayoutEvaluatorStateInitializer>();
        var accessChecker = _collection.Mock<IDataElementAccessChecker>();
        accessChecker.Setup(x => x.CanRead(It.IsAny<Instance>(), It.IsAny<DataType>())).ReturnsAsync(true);
        accessChecker
            .Setup(x => x.CanDelete(It.IsAny<Instance>(), It.IsAny<DataType>(), It.IsAny<Guid>(), null))
            .ReturnsAsync(userCanDelete);
    }
}
