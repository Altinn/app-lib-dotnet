using System.Runtime.CompilerServices;

namespace Altinn.App.Analyzers.Tests.Fixtures;

public class InjectedException : Exception
{
    public InjectedException(string message, [CallerMemberName] string caller = "")
        : base($"{caller}: {message}") { }
}
