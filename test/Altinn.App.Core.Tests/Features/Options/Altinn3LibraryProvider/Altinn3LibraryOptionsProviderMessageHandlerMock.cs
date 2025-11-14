namespace Altinn.App.Core.Tests.Features.Options.Altinn3LibraryProvider;

public class Altinn3LibraryOptionsProviderMessageHandlerMock : DelegatingHandler
{
    // Instrumentation to test that caching works
    private int _callCount = 0;
    public int CallCount => _callCount;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        Interlocked.Increment(ref _callCount);
        return Task.FromResult(Altinn3LibraryOptionsProviderTestData.GetNbEnResponseMessage());
    }
}
