using Altinn.App.Integration;
using Altinn.App.Integration.FrontendTests.GeneratedClient;
using Xunit.Abstractions;

namespace AppTest;

public static class RunInDocker
{
    public static async Task RunInDockerAsync()
    {
        var appFixture = new FrontendFixture();
        await appFixture.InitializeAsync();
        try
        {
            var appClient = appFixture.GetAppClient();
            FrontendTestsRunner runner = new(appClient, Console.WriteLine);
            await runner.RunMultipleSteps();
        }
        catch (Exception ex)
        {
            await Task.Delay(TimeSpan.FromMinutes(10));
            Console.WriteLine("Exception: ");
            Console.WriteLine(ex);
        }
        finally
        {
            await appFixture.DisposeAsync();
        }
    }
}

public class ConsoleSink : IMessageSink
{
    public bool OnMessage(IMessageSinkMessage message)
    {
        Console.WriteLine(message.ToString());
        return true;
    }
}

class FrontendFixture : RunningAppFixture
{
    public FrontendFixture()
        : base("frontend-test", new ConsoleSink()) { }
}
