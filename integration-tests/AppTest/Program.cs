using Altinn.App.Integration.FrontendTests.GeneratedClient;

// await RunInDocker.RunInDockerAsync();

var client = new HttpClient() { BaseAddress = new Uri("http://local.altinn.cloud") };

FrontendTestsRunner runner = new(client, Console.WriteLine);
await runner.RunMultipleSteps();
