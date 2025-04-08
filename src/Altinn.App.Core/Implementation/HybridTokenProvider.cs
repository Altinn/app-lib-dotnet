using Altinn.App.Core.Internal.Auth;

namespace Altinn.App.Core.Implementation;

/// <summary>
///A Hybrid Token provider getting the token from a stateful service implemented the same way as httpcontextaccesor
public class HybridTokenProvider : ITokenProvider
{
    private ITokenProvider _defaultProvider;
    private readonly HybridTokenProviderStateContext _stateContext;

    /// <summary>
    /// Constructor for the HybridTokenProvider
    /// </summary>
    /// <param name="defaultProvider">The default provider to use if context value is not set</param>
    public HybridTokenProvider(ITokenProvider defaultProvider)
    {
        _defaultProvider = defaultProvider;
    }

    public string GetToken()
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// This class is used to store the state for the Hybrid token provider
/// </summary>
/// <param name="TokenValue"></param>
public record HybridTokenProviderState(string TokenValue);


/// <summary>
/// 
/// </summary>
public class HybridTokenProviderStateContext
{
    private readonly AsyncLocal<HybridTokenProviderState> _currentState = new AsyncLocal<HybridTokenProviderState>();

    // Default state when nothing is set
    private readonly HybridTokenProviderState _defaultState = new HybridTokenProviderState(string.Empty);

    /// <summary>
    /// Property to get the current state
    /// </summary>
    public HybridTokenProviderState Current => _currentState.Value ?? _defaultState;

    /// <summary>
    /// Method to set state with proper scoping
    /// </summary>
    /// <param name="newState"></param>
    /// <returns></returns>
    public IDisposable SetCurrent(HybridTokenProviderState newState)
    {
        return new HybridTokenStateScope(this, newState);
    }

    // Nested private class to handle the scoping
    private class HybridTokenStateScope : IDisposable
    {
        private readonly HybridTokenProviderStateContext _context;
        private readonly HybridTokenProviderState _previousState;

        /// <summary>
        /// Constructor to set the new state
        /// </summary>
        /// <param name="context"></param>
        /// <param name="newState"></param>
        public HybridTokenStateScope(HybridTokenProviderStateContext context, HybridTokenProviderState newState)
        {
            _context = context;
            _previousState = _context._currentState.Value;
            _context._currentState.Value = newState;
        }

        public void Dispose()
        {
            _context._currentState.Value = _previousState;
        }
    }
}
