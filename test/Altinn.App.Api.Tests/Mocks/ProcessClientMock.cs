using Altinn.App.Core.Configuration;
using Altinn.App.Core.Internal.Process;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Api.Tests.Mocks;

public class ProcessClientMock : IProcessClient
{
    private readonly AppSettings _appSettings;

    public ProcessClientMock(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }

    public Stream GetProcessDefinition()
    {
        string bpmnFilePath = Path.Join(
            _appSettings.AppBasePath,
            _appSettings.ConfigurationFolder,
            _appSettings.ProcessFolder,
            _appSettings.ProcessFileName
        );

        Stream processModel = File.OpenRead(bpmnFilePath);
        return processModel;
    }

    public Task<ProcessHistoryList> GetProcessHistory(string instanceGuid, string instanceOwnerPartyId)
    {
        throw new NotImplementedException();
    }
}
