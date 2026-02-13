using System.Runtime.CompilerServices;

namespace Altinn.App.Core.Tests.TestUtils;

public static class TestAttributeHelper
{
    public static string AltinnAppTestsBasePath([CallerFilePath] string? callerFilePath = null)
    {
        if (callerFilePath is null)
        {
            throw new Exception("Caller path is null");
        }
        var testUtilsDirectoryPath = Path.GetDirectoryName(callerFilePath);
        if (testUtilsDirectoryPath is null)
        {
            throw new Exception("Caller path is null");
        }
        var callerDirectoryPath = Path.GetDirectoryName(testUtilsDirectoryPath);
        if (callerDirectoryPath is null)
        {
            throw new Exception("Caller path is null");
        }

        return callerDirectoryPath;
    }
}
