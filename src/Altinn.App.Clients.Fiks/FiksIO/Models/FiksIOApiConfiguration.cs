using KS.Fiks.IO.Send.Client.Configuration;

namespace Altinn.App.Clients.Fiks.FiksIO.Models;

/// <summary>
/// Represents the Fiks IO API configuration.
/// </summary>
public sealed class FiksIOApiConfiguration : ApiConfiguration
{
    /// <summary>
    /// The host or base URL, defaults to 'api.fiks.ks.no'.
    /// </summary>
    public new string? Host { get; set; }

    /// <summary>
    /// The port number, defaults to 443.
    /// </summary>
    public new int? Port { get; set; }

    /// <summary>
    /// The schema, defaults to https.
    /// </summary>
    public new string? Scheme { get; set; }
}
