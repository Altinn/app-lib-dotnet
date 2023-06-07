using Altinn.App.Core.Internal.Process;
using Moq;

namespace Altinn.App.Core.Tests.Internal.Process.TestUtils;

internal static class ProcessTestUtils
{
    private static readonly string TestDataPath = Path.Combine("Internal", "Process", "TestData");
    
    internal static ProcessReader SetupProcessReader(string bpmnfile)
    {
        Mock<IProcessClient> processServiceMock = new Mock<IProcessClient>();
        var s = new FileStream(Path.Combine(TestDataPath, bpmnfile), FileMode.Open, FileAccess.Read);
        processServiceMock.Setup(p => p.GetProcessDefinition()).Returns(s);
        return new ProcessReader(processServiceMock.Object);
    }
}
