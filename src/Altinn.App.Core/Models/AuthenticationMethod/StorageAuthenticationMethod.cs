namespace Altinn.App.Core.Models.AuthenticationMethod;

/// <summary>
/// Represents the method of authentication to be used for making requests to the Storage service.
/// </summary>
public sealed record StorageAuthenticationMethod
{
    /// <inheritdoc cref="AuthenticationMethod.CurrentUser"/>
    public static StorageAuthenticationMethod CurrentUser() => new(AuthenticationMethod.CurrentUser());

    /// <inheritdoc cref="AuthenticationMethod.ServiceOwner()"/>
    public static StorageAuthenticationMethod ServiceOwner() => new(AuthenticationMethod.ServiceOwner());

    /// <inheritdoc cref="AuthenticationMethod.ServiceOwner(string[])"/>
    public static StorageAuthenticationMethod ServiceOwner(params string[] additionalScopes) =>
        new(AuthenticationMethod.ServiceOwner(additionalScopes));

    /// <inheritdoc cref="AuthenticationMethod.Custom"/>
    public static StorageAuthenticationMethod Custom(Func<Task<JwtToken>> tokenProvider) =>
        new(AuthenticationMethod.Custom(tokenProvider));

    internal AuthenticationMethod Request { get; }

    private StorageAuthenticationMethod(AuthenticationMethod request)
    {
        Request = request;
    }

    /// <summary>
    /// Implicit conversion from <see cref="StorageAuthenticationMethod"/> to <see cref="AuthenticationMethod"/>.
    /// </summary>
    public static implicit operator AuthenticationMethod(StorageAuthenticationMethod storageAuthenticationMethod) =>
        storageAuthenticationMethod.Request;
}
