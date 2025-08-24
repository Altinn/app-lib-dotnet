namespace Altinn.App.Integration.Tests;

public partial class AppFixture : IAsyncDisposable
{
    private ConnectivityOperations? _connectivity;
    internal ConnectivityOperations Connectivity
    {
        get
        {
            if (_connectivity == null)
            {
                _connectivity = new ConnectivityOperations(this);
            }
            return _connectivity;
        }
    }

    internal sealed class ConnectivityOperations(AppFixture fixture)
    {
        private readonly AppFixture _fixture = fixture;

        /// <summary>
        /// Tests container-to-container connectivity by calling the app's PDF diagnostic endpoint.
        /// This verifies that the app container can reach the PDF service container via host.docker.internal.
        /// </summary>
        public async Task<string> Pdf()
        {
            var client = _fixture.GetAppClient();
            using var response = await client.GetAsync($"/ttd/{_fixture._app}/diagnostics/connectivity/pdf");
            Assert.True(response.IsSuccessStatusCode, "Failed to check app container PDF connectivity");
            var content = await response.Content.ReadAsStringAsync();
            return content;
        }
    }
}

internal sealed record ConnectivityResult(
    bool Success,
    int StatusCode,
    string Url,
    string? ResponseContent,
    string Message,
    string? Exception
);
