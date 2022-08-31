#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Altinn.App.PlatformServices.Interface;

/// <summary>
/// Defines the required operations on a client of the PDF generator service.
/// </summary>
public interface IPdfGeneratorClient
{
    /// <summary>
    /// Generates a PDF.
    /// </summary>
    /// <returns>A stream with the binary content of the generated PDF</returns>
    Task<Stream> GeneratePdf(Uri uri, CancellationToken ct);
}
