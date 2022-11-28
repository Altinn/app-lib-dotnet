#nullable enable

using System.Collections.Generic;

namespace Altinn.App.Core.Models;

/// <summary>
/// This class is created to match the input required to generate a PDF by the PDF generator service.
/// </summary>
internal class PdfGeneratorRequest
{
    /// <summary>
    /// The Url that the PDF generator will used to obtain the HTML needed to created the PDF.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// PDF generator request options.
    /// </summary>
    public PdfGeneratorRequestOptions Options { get; set; } = new();

    /// <summary>
    /// Indicate whether javascript should be enabled. Default is true. This is also required when the HTML
    /// is created by a React application.
    /// </summary>
    public bool SetJavaScriptEnabled { get; set; } = true;

    /// <summary>
    /// Defines how puppeteer should wait before starting triggering PDF rendering.
    /// </summary>
    public object? WaitFor { get; set; } = null;

    /// <summary>
    /// Provides a list of cookies Puppeteer will need to create before sending the request.
    /// </summary>
    public List<PdfGeneratorCookieOptions> Cookies { get; set; } = new();
}

/// <summary>
/// This class is created to match the PDF generator options used by the PDF generator.
/// </summary>
internal class PdfGeneratorRequestOptions
{
    /// <summary>
    /// Indicate whether header and footer should be included.
    /// </summary>
    public bool DisplayHeaderFooter { get; set; } = true;

    /// <summary>
    /// Indicate wheter the background should be included.
    /// </summary>
    public bool PrintBackground { get; set; } = false;

    /// <summary>
    /// Defines the page size. Default is A4.
    /// </summary>
    public string Format { get; set; } = "A4";

    /// <summary>
    /// Defines the page margins. Default is "0.4in" on all sides.
    /// </summary>
    public MarginOptions Margin { get; set; } = new();
}

/// <summary>
/// This class is created to match the PDF generator cookie options.
/// </summary>
internal class PdfGeneratorCookieOptions
{
    /// <summary>
    /// The name of the cookie.
    /// </summary>
    public string Name { get; } = "AltinnStudioRuntime";

    /// <summary>
    /// The cookie content.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The cookie domain.
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// The cookie sameSite settings.
    /// </summary>
    public string SameSite { get; } = "Lax";
}

/// <summary>
/// This class is created to match the PDF generator marking options.
/// </summary>
internal class MarginOptions
{
    /// <summary>
    /// Top margin, accepts values labeled with units.
    /// </summary>
    public string Top { get; set; } = "0.4in";

    /// <summary>
    /// Left margin, accepts values labeled with units
    /// </summary>
    public string Left { get; set; } = "0.4in";

    /// <summary>
    /// Bottom margin, accepts values labeled with units
    /// </summary>
    public string Bottom { get; set; } = "0.4in";

    /// <summary>
    /// Right margin, accepts values labeled with units
    /// </summary>
    public string Right { get; set; } = "0.4in";
}