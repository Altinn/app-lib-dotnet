using Altinn.App.Api.Controllers;
using Altinn.App.Core.Interface;
using Altinn.Platform.Storage.Interface.Models;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Altinn.App.Api.Tests.Controllers
{
    public class FileScanControllerTests
    {
        [Fact]
        public async void AtLeasOneDataElementInfected()
        {
            const string org = "org";
            const string app = "app";
            const int instanceOwnerPartyId = 12345;
            var instanceId = Guid.NewGuid();

            Instance instance = new Instance
            {
                Id = instanceOwnerPartyId.ToString() + "/" + instanceId.ToString(),
                Process = null,
                Data = new List<DataElement>() 
                {
                    new() { Id = Guid.NewGuid().ToString(), FileScanResult = Platform.Storage.Interface.Enums.FileScanResult.Infected }
                }
            };


            var instanceClientMock = new Mock<IInstance>();
            instanceClientMock
                .Setup(e => e.GetInstance(app, org, instanceOwnerPartyId, instanceId))
                .Returns(Task.FromResult<Instance>(instance));

            FileScanController fileScanController = new FileScanController(instanceClientMock.Object);

            var fileScanResults = await fileScanController.GetFileScanResults(org, app, instanceOwnerPartyId, instanceId);

            fileScanResults.Value?.FileScanResult.Should().Be(Platform.Storage.Interface.Enums.FileScanResult.Infected);
        }
    }
}
