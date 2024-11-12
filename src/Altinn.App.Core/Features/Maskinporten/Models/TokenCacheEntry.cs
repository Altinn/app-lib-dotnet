using System.ComponentModel;

namespace Altinn.App.Core.Features.Maskinporten.Models;

// `ImmutableObject` prevents serialization with HybridCache
[ImmutableObject(true)]
internal sealed record TokenCacheEntry(TokenWrapper Token, TimeSpan ExpiresIn, bool HasSetExpiration);
