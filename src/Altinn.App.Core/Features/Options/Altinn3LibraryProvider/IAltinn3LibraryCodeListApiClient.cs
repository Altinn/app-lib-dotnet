namespace Altinn.App.Core.Features.Options.Altinn3LibraryProvider;

public interface IAltinn3LibraryCodeListApiClient
{
    Task<Altinn3LibraryCodeListResponse> GetAltinn3LibraryCodeLists(
        string org,
        string codeListId,
        string version = null,
        CancellationToken cancellationToken = default
    );
}
