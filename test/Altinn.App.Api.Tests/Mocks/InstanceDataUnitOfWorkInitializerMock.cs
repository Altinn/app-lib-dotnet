using Altinn.App.Core.Configuration;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.App.Api.Tests.Mocks;

internal class InstanceDataUnitOfWorkInitializerMock
{
    public InstanceDataUnitOfWorkInitializer InstanceDataUnitOfWorkInitializer { get; }
    public Mock<IDataClient> DataClientMock { get; }
    public Mock<IInstanceClient> InstanceClientMock { get; }
    public Mock<IAppMetadata> ApplicationMetadataMock { get; }
    public Mock<IAppModel> AppModelMock { get; }
    public Mock<ModelSerializationService> ModelSerializationServiceMock { get; }
    public Mock<IAppResources> AppResourcesMock { get; }
    public Mock<IOptions<FrontEndSettings>> FrontEndSettingsMock { get; }

    private InstanceDataUnitOfWorkInitializerMock(
        Mock<IDataClient> dataClientMock,
        Mock<IInstanceClient> instanceClientMock,
        Mock<IAppMetadata> applicationMetadataMock,
        Mock<IAppModel> appModelMock,
        Mock<ModelSerializationService> modelSerializationServiceMock,
        Mock<IAppResources> appResourcesMock,
        Mock<IOptions<FrontEndSettings>> frontEndSettingsMock
    )
    {
        DataClientMock = dataClientMock;
        InstanceClientMock = instanceClientMock;
        ApplicationMetadataMock = applicationMetadataMock;
        AppModelMock = appModelMock;
        ModelSerializationServiceMock = modelSerializationServiceMock;
        AppResourcesMock = appResourcesMock;
        FrontEndSettingsMock = frontEndSettingsMock;

        InstanceDataUnitOfWorkInitializer = new InstanceDataUnitOfWorkInitializer(
            DataClientMock.Object,
            InstanceClientMock.Object,
            ApplicationMetadataMock.Object,
            ModelSerializationServiceMock.Object,
            AppResourcesMock.Object,
            FrontEndSettingsMock.Object
        );
    }

    public static InstanceDataUnitOfWorkInitializerMock Create()
    {
        Mock<IDataClient> dataClientMock = new();
        Mock<IInstanceClient> instanceClientMock = new();
        Mock<IAppMetadata> applicationMetadataMock = new();
        Mock<IAppModel> appModelMock = new();
        Mock<ModelSerializationService> modelSerializationServiceMock = new(appModelMock.Object, null!);
        Mock<IAppResources> appResourcesMock = new();
        Mock<IOptions<FrontEndSettings>> frontEndSettingsMock = new();

        return new InstanceDataUnitOfWorkInitializerMock(
            dataClientMock,
            instanceClientMock,
            applicationMetadataMock,
            appModelMock,
            modelSerializationServiceMock,
            appResourcesMock,
            frontEndSettingsMock
        );
    }
}
