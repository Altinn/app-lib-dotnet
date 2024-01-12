using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process.ServiceTasks;
using Altinn.App.Core.Models;
using Altinn.App.Core.Tests.Internal.Process.ServiceTasks.TestData;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.FeatureManagement;
using Moq;
using Xunit;

namespace Altinn.App.Core.Tests.Internal.Process.ServiceTasks;

public class PdfServiceTaskTests
{
    private readonly Mock<IAppMetadata> _appMetadata;
    private readonly Mock<IFeatureManager> _featureManager;
    private readonly Mock<IPdfService> _pdfService;
    private readonly Mock<IAppModel> _appModel;

    public PdfServiceTaskTests()
    {
        _appMetadata = new Mock<IAppMetadata>();
        _featureManager = new Mock<IFeatureManager>();
        _pdfService = new Mock<IPdfService>();
        _appModel = new Mock<IAppModel>();
    }
    
    [Fact]
    public async Task Execute_calls_only_appmetadata_and_featureflags_service_if_no_datatypes_connected_to_task()
    {
        Instance i = new Instance();
        SetupAppMetadataWithDataTypes();
        PdfServiceTask pst = new PdfServiceTask(_appMetadata.Object, _featureManager.Object, _pdfService.Object, _appModel.Object);
        await pst.Execute("Task_1", i);
        _appMetadata.Verify(am => am.GetApplicationMetadata(), Times.Once);
        _featureManager.Verify(fm => fm.IsEnabledAsync(FeatureFlags.NewPdfGeneration), Times.Once);
        _appMetadata.VerifyNoOtherCalls();
        _featureManager.VerifyNoOtherCalls();
        _pdfService.VerifyNoOtherCalls();
        _appModel.VerifyNoOtherCalls();
    }
    
    [Fact]
    public async Task Execute_calls_new_pdf_service_when_featureflag_NewPdfGeneration_is_true()
    {
        Instance i = new Instance()
        {
            Data =
            [
                new DataElement()
                {
                    DataType = "DataType_1"
                },
            ]
        };
        SetupAppMetadataWithDataTypes([
            new DataType
            {
                Id = "DataType_1",
                TaskId = "Task_1",
                AppLogic = new ApplicationLogic()
                {
                    ClassRef = "DataType_1"
                },
                EnablePdfCreation = true
            },
        ]);
        _featureManager.Setup(fm => fm.IsEnabledAsync(FeatureFlags.NewPdfGeneration)).ReturnsAsync(true);
        PdfServiceTask pst = new PdfServiceTask(_appMetadata.Object, _featureManager.Object, _pdfService.Object, _appModel.Object);
        await pst.Execute("Task_1", i);
        _appMetadata.Verify(am => am.GetApplicationMetadata(), Times.Once);
        _featureManager.Verify(fm => fm.IsEnabledAsync(FeatureFlags.NewPdfGeneration), Times.Once);
        _pdfService.Verify(ps => ps.GenerateAndStorePdf(i, "Task_1", CancellationToken.None), Times.Once);
        _appMetadata.VerifyNoOtherCalls();
        _featureManager.VerifyNoOtherCalls();
        _pdfService.VerifyNoOtherCalls();
        _appModel.VerifyNoOtherCalls();
    }
    
    [Fact]
    public async Task Execute_when_featureflag_NewPdfGeneration_is_true_new_pdf_service_is_call_only_once()
    {
        Instance i = new Instance()
        {
            Data =
            [
                new DataElement()
                {
                    DataType = "DataType_1"
                },
                new DataElement()
                {
                    DataType = "DataType_1"
                },
                new DataElement()
                {
                    DataType = "DataType_2"
                },
                new DataElement()
                {
                    DataType = "DataType_2"
                },
            ]
        };
        SetupAppMetadataWithDataTypes([
            new DataType
            {
                Id = "DataType_1",
                TaskId = "Task_1",
                AppLogic = new ApplicationLogic()
                {
                    ClassRef = "DataType_1"
                },
                EnablePdfCreation = true
            },
            new DataType
            {
                Id = "DataType_2",
                TaskId = "Task_1",
                AppLogic = new ApplicationLogic()
                {
                    ClassRef = "DataType_2"
                },
                EnablePdfCreation = true
            },
        ]);
        _featureManager.Setup(fm => fm.IsEnabledAsync(FeatureFlags.NewPdfGeneration)).ReturnsAsync(true);
        PdfServiceTask pst = new PdfServiceTask(_appMetadata.Object, _featureManager.Object, _pdfService.Object, _appModel.Object);
        await pst.Execute("Task_1", i);
        _appMetadata.Verify(am => am.GetApplicationMetadata());
        _featureManager.Verify(fm => fm.IsEnabledAsync(FeatureFlags.NewPdfGeneration));
        _pdfService.Verify(ps => ps.GenerateAndStorePdf(i, "Task_1", CancellationToken.None), Times.Once);
        _appMetadata.VerifyNoOtherCalls();
        _featureManager.VerifyNoOtherCalls();
        _pdfService.VerifyNoOtherCalls();
        _appModel.VerifyNoOtherCalls();
    }
    
    [Fact]
    public async Task Execute_when_featureflag_NewPdfGeneration_is_never_called_if_no_dataelements_for_datatype()
    {
        Instance i = new Instance()
        {
            Data = []
        };
        SetupAppMetadataWithDataTypes([
            new DataType
            {
                Id = "DataType_1",
                TaskId = "Task_1",
                AppLogic = new ApplicationLogic()
                {
                    ClassRef = "DataType_1"
                },
                EnablePdfCreation = true
            },
            new DataType
            {
                Id = "DataType_2",
                TaskId = "Task_1",
                AppLogic = new ApplicationLogic()
                {
                    ClassRef = "DataType_2"
                },
                EnablePdfCreation = true
            },
        ]);
        _featureManager.Setup(fm => fm.IsEnabledAsync(FeatureFlags.NewPdfGeneration)).ReturnsAsync(true);
        PdfServiceTask pst = new PdfServiceTask(_appMetadata.Object, _featureManager.Object, _pdfService.Object, _appModel.Object);
        await pst.Execute("Task_1", i);
        _appMetadata.Verify(am => am.GetApplicationMetadata());
        _featureManager.Verify(fm => fm.IsEnabledAsync(FeatureFlags.NewPdfGeneration));
        _appMetadata.VerifyNoOtherCalls();
        _featureManager.VerifyNoOtherCalls();
        _pdfService.VerifyNoOtherCalls();
        _appModel.VerifyNoOtherCalls();
    }
    
    [Fact]
    public async Task Execute_calls_legacy_pdf_service_when_featureflag_NewPdfGeneration_is_false()
    {
        DataElement d = new DataElement()
        {
            Id = "DataElement_1",
            DataType = "DataType_1"
        };
        Instance i = new Instance()
        {
            Data =
            [
                d,
            ]
        };
        SetupAppMetadataWithDataTypes([
            new DataType
            {
                Id = "DataType_1",
                TaskId = "Task_1",
                AppLogic = new ApplicationLogic()
                {
                    ClassRef = "Altinn.App.Core.Tests.Internal.Process.ServiceTasks.TestData.DummyDataType"
                },
                EnablePdfCreation = true
            },
        ]);
        _featureManager.Setup(fm => fm.IsEnabledAsync(FeatureFlags.NewPdfGeneration)).ReturnsAsync(false);
        _appModel.Setup(am => am.GetModelType("Altinn.App.Core.Tests.Internal.Process.ServiceTasks.TestData.DummyDataType")).Returns(typeof(DummyDataType));
        PdfServiceTask pst = new PdfServiceTask(_appMetadata.Object, _featureManager.Object, _pdfService.Object, _appModel.Object);
        await pst.Execute("Task_1", i);
        _appMetadata.Verify(am => am.GetApplicationMetadata(), Times.Once);
        _featureManager.Verify(fm => fm.IsEnabledAsync(FeatureFlags.NewPdfGeneration), Times.Once);
        _appModel.Verify(am => am.GetModelType("Altinn.App.Core.Tests.Internal.Process.ServiceTasks.TestData.DummyDataType"), Times.Once);
        _pdfService.Verify(ps => ps.GenerateAndStoreReceiptPDF(i, "Task_1", d, typeof(DummyDataType)), Times.Once);
        _appMetadata.VerifyNoOtherCalls();
        _featureManager.VerifyNoOtherCalls();
        _pdfService.VerifyNoOtherCalls();
        _appModel.VerifyNoOtherCalls();
    }
    
    [Fact]
    public async Task Execute_does_not_call_pdfservice_if_generate_pdf_are_false_for_all_datatypes()
    {
        DataElement d = new DataElement()
        {
            Id = "DataElement_1",
            DataType = "DataType_1"
        };
        Instance i = new Instance()
        {
            Data =
            [
                d,
            ]
        };
        SetupAppMetadataWithDataTypes([
            new DataType
            {
                Id = "DataType_1",
                TaskId = "Task_1",
                AppLogic = new ApplicationLogic()
                {
                    ClassRef = "Altinn.App.Core.Tests.Internal.Process.ServiceTasks.TestData.DummyDataType"
                },
                EnablePdfCreation = false
            },
        ]);
        PdfServiceTask pst = new PdfServiceTask(_appMetadata.Object, _featureManager.Object, _pdfService.Object, _appModel.Object);
        await pst.Execute("Task_1", i);
        _appMetadata.Verify(am => am.GetApplicationMetadata(), Times.Once);
        _featureManager.Verify(fm => fm.IsEnabledAsync(FeatureFlags.NewPdfGeneration), Times.Once);
        _appMetadata.VerifyNoOtherCalls();
        _featureManager.VerifyNoOtherCalls();
        _pdfService.VerifyNoOtherCalls();
        _appModel.VerifyNoOtherCalls();
    }
    
    [Fact]
    public async Task Execute_does_not_call_pdfservice_if_generate_pdf_are_false_for_all_datatypes_nde_pdf_flag_true()
    {
        DataElement d = new DataElement()
        {
            Id = "DataElement_1",
            DataType = "DataType_1"
        };
        Instance i = new Instance()
        {
            Data =
            [
                d,
            ]
        };
        SetupAppMetadataWithDataTypes([
            new DataType
            {
                Id = "DataType_1",
                TaskId = "Task_1",
                AppLogic = new ApplicationLogic()
                {
                    ClassRef = "Altinn.App.Core.Tests.Internal.Process.ServiceTasks.TestData.DummyDataType"
                },
                EnablePdfCreation = false
            },
        ]);
        _featureManager.Setup(fm => fm.IsEnabledAsync(FeatureFlags.NewPdfGeneration)).ReturnsAsync(true);
        PdfServiceTask pst = new PdfServiceTask(_appMetadata.Object, _featureManager.Object, _pdfService.Object, _appModel.Object);
        await pst.Execute("Task_1", i);
        _appMetadata.Verify(am => am.GetApplicationMetadata(), Times.Once);
        _featureManager.Verify(fm => fm.IsEnabledAsync(FeatureFlags.NewPdfGeneration), Times.Once);
        _appMetadata.VerifyNoOtherCalls();
        _featureManager.VerifyNoOtherCalls();
        _pdfService.VerifyNoOtherCalls();
        _appModel.VerifyNoOtherCalls();
    }
    
    [Fact]
    public async Task Execute_does_not_call_pdfService_for_legacy_generation_if_newpdfservice_flag_false()
    {
        Instance i = new Instance()
        {
            Data = []
        };
        SetupAppMetadataWithDataTypes([
            new DataType
            {
                Id = "DataType_1",
                TaskId = "Task_1",
                AppLogic = new ApplicationLogic()
                {
                    ClassRef = "DataType_1"
                },
                EnablePdfCreation = true
            },
            new DataType
            {
                Id = "DataType_2",
                TaskId = "Task_1",
                AppLogic = new ApplicationLogic()
                {
                    ClassRef = "DataType_2"
                },
                EnablePdfCreation = true
            },
        ]);
        _featureManager.Setup(fm => fm.IsEnabledAsync(FeatureFlags.NewPdfGeneration)).ReturnsAsync(false);
        PdfServiceTask pst = new PdfServiceTask(_appMetadata.Object, _featureManager.Object, _pdfService.Object, _appModel.Object);
        await pst.Execute("Task_1", i);
        _appMetadata.Verify(am => am.GetApplicationMetadata());
        _featureManager.Verify(fm => fm.IsEnabledAsync(FeatureFlags.NewPdfGeneration));
        _appMetadata.VerifyNoOtherCalls();
        _featureManager.VerifyNoOtherCalls();
        _pdfService.VerifyNoOtherCalls();
        _appModel.VerifyNoOtherCalls();
    }
    
    private void SetupAppMetadataWithDataTypes(List<DataType>? dataTypes = null)
    {
        _appMetadata.Setup(am => am.GetApplicationMetadata()).ReturnsAsync(new ApplicationMetadata("ttd/test")
        {
            DataTypes = dataTypes ?? new List<DataType> { }
        });
    }
}