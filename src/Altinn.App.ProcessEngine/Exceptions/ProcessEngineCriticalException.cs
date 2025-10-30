//using Altinn.App.Core.Exceptions;

namespace Altinn.App.ProcessEngine.Exceptions;

internal sealed class ProcessEngineCriticalException : Exception
{
    public ProcessEngineCriticalException(string message)
        : base(message) { }
}
