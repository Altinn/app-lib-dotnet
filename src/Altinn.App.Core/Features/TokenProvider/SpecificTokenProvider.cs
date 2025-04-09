using Altinn.App.Core.Internal.Auth;

namespace Altinn.App.Core.Features.TokenProvider;

/// <summary>
/// A Specific Token provider getting the token from a stateful service implemented the same way as httpcontextaccesor with asynclocal
/// </summary>
public class SpecificTokenProvider : IUserTokenProvider
{
    private IUserTokenProvider _defaultProvider;
    private readonly SpecificTokenProviderStateContext _stateContext;

    /// <summary>
    /// The token provider is initialized with a default provider and a state context.
    /// </summary>
    /// <param name="defaultProvider"></param>
    /// <param name="stateContext"></param>
    public SpecificTokenProvider(IUserTokenProvider defaultProvider, SpecificTokenProviderStateContext stateContext)
    {
        _defaultProvider = defaultProvider;
        _stateContext = stateContext;
    }

    /// <summary>
    /// Gets the token from the current context
    /// </summary>
    /// <returns>the token to use</returns>
    public string GetUserToken()
    {
        if (string.IsNullOrEmpty(_stateContext.Current.TokenValue))
        {
            return _defaultProvider.GetUserToken();
        }
        else
        {
            return _stateContext.Current.TokenValue;
        }
    }
}
