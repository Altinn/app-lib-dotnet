namespace Altinn.App.Core.Features.TokenProvider;

#nullable enable
/// <summary>
/// This class is used to store and access the state for the Specific token provider
/// </summary>
public class SpecificTokenProviderStateContext
{
    private readonly AsyncLocal<SpecificTokenProviderState> _currentState =
        new AsyncLocal<SpecificTokenProviderState>();

    // Default state when nothing is set
    private readonly SpecificTokenProviderState _defaultState = new SpecificTokenProviderState(string.Empty);

    /// <summary>
    /// Property to get the current state
    /// </summary>
    public SpecificTokenProviderState Current => _currentState.Value ?? _defaultState;

    /// <summary>
    /// Method to use state with proper scoping
    /// </summary>
    /// <param name="newState"></param>
    /// <returns>idisposable token state</returns>
    public IDisposable UseTokenState(SpecificTokenProviderState newState)
    {
        return new HybridTokenStateScope(this, newState);
    }

    /// <summary>
    /// Sets the current token value
    /// </summary>
    /// <param name="value"></param>
    /// <returns> idisposable token state</returns>
    public IDisposable UseToken(string value)
    {
        return UseTokenState(new SpecificTokenProviderState(value));
    }

    // Nested private class to handle the scoping
    private class HybridTokenStateScope : IDisposable
    {
        private readonly SpecificTokenProviderStateContext _context;
        private readonly SpecificTokenProviderState _previousState = new SpecificTokenProviderState(string.Empty);

        /// <summary>
        /// Constructor to set the new state
        /// </summary>
        /// <param name="context"></param>
        /// <param name="newState"></param>
        public HybridTokenStateScope(SpecificTokenProviderStateContext context, SpecificTokenProviderState newState)
        {
            _context = context;
            if (!string.IsNullOrEmpty(context.Current.TokenValue))
            {
                _previousState = context.Current;
            }
            _context._currentState.Value = newState;
        }

        public void Dispose()
        {
            _context._currentState.Value = _previousState;
        }
    }
}
