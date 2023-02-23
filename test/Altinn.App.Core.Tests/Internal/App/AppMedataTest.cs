using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.App;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Altinn.App.Core.Tests.Internal.App
{
    public class AppMedataTest
    {
        [Fact]
        public void GetApplicationMetadata_desrializes_file_from_disk()
        {
            AppSettings appSettings = new AppSettings();
            IAppMetadata appMetadata = new AppMetadata(Options.Create<AppSettings>(appSettings), new AppFeatures(), new NullLogger<AppMetadata>());
            
        }
    }
}
