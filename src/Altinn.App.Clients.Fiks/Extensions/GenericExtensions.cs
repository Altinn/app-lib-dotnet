using Altinn.App.Clients.Fiks.Exceptions;

namespace Altinn.App.Clients.Fiks.Extensions;

internal static class GenericExtensions
{
    public static T VerifiedNotNull<T>(this T? value)
    {
        if (value is null)
            throw new FiksArkivException($"Value of type {typeof(T).Name} is unexpectedly null.");

        return value;
    }
}
