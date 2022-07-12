using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Altinn.App.Common.Models;
using Altinn.App.PlatformServices.Models;
using Altinn.App.Services.Implementation;
using Altinn.App.Services.Interface;
using Moq;
using Xunit;

namespace Altinn.App.PlatformServices.Tests.Implementation
{
    public class DefaultPageOrderTest
    {
        private readonly Mock<IAltinnApp> altinnAppMock;
        private readonly Mock<IAppResources> appResourcesMock;

        public DefaultPageOrderTest()
        {
            altinnAppMock = new Mock<IAltinnApp>();
            appResourcesMock = new Mock<IAppResources>();
        }

        [Fact]
        public async Task GetPageOrder_Returns_LayoutSettingsForSet_when_layoutSetId_is_defined()
        {
            // Arrange
            AppIdentifier appIdentifier = new AppIdentifier("ttd", "best-app");
            Guid guid = Guid.NewGuid();
            InstanceIdentifier instanceIdentifier = InstanceIdentifier.NoInstance;
            string layoutSetId = "layoutSetId";
            string currentPage = "page1";
            string dataTypeId = "dataTypeId";
            object formData = new object();

            List<string> expected = new List<string> { "page2", "page3" };
            appResourcesMock.Setup(ar => ar.GetLayoutSettingsForSet(layoutSetId)).Returns(new LayoutSettings { Pages = new Pages { Order = expected } });

            // Act
            DefaultPageOrder target = new DefaultPageOrder(altinnAppMock.Object, appResourcesMock.Object);

            List<string> actual = await target.GetPageOrder(appIdentifier, instanceIdentifier, layoutSetId, currentPage, dataTypeId, formData);

            // Assert
            Assert.Equal(expected, actual);
            appResourcesMock.Verify(ar => ar.GetLayoutSettingsForSet(layoutSetId), Times.Once);
            altinnAppMock.VerifyNoOtherCalls();
            appResourcesMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetPageOrder_Returns_LayoutSettings_layoutSetId_is_null()
        {
            // Arrange
            AppIdentifier appIdentifier = new AppIdentifier("ttd", "best-app");
            Guid guid = Guid.NewGuid();
            InstanceIdentifier instanceIdentifier = InstanceIdentifier.NoInstance;
            string layoutSetId = null;
            string currentPage = "page1";
            string dataTypeId = "dataTypeId";
            object formData = new object();

            List<string> expected = new List<string> { "page2", "page3" };
            appResourcesMock.Setup(ar => ar.GetLayoutSettings()).Returns(new LayoutSettings { Pages = new Pages { Order = expected } });

            // Act
            DefaultPageOrder target = new DefaultPageOrder(altinnAppMock.Object, appResourcesMock.Object);

            List<string> actual = await target.GetPageOrder(appIdentifier, instanceIdentifier, layoutSetId, currentPage, dataTypeId, formData);

            // Assert
            Assert.Equal(expected, actual);
            appResourcesMock.Verify(ar => ar.GetLayoutSettings(), Times.Once);
            altinnAppMock.VerifyNoOtherCalls();
            appResourcesMock.VerifyNoOtherCalls();
        }
    }
}