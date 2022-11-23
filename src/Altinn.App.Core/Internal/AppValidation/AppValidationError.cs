using System.Text.Json;
using Altinn.App.Core.Internal.AppFiles;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Layout.Components;

namespace Altinn.App.Core.Internal.AppValidation;

/// <summary>
/// Information used for pretty printing an AppValidationError
/// </summary>
public class AppValidationError
{
    /// <summary>
    /// Helper to print erors to console.
    /// </summary>
    public static void PrintErors(List<AppValidationError> errors)
    {
        try
        {
            if (errors.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.Write("  Errors found in app validation:  ");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine();

                foreach (var error in errors)
                {
                    Console.ForegroundColor = ConsoleColor.DarkBlue;
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.Write(error.ErrorLocation?.PrintConsole());
                    Console.ResetColor();

                    Console.Write(' ');
                    Console.Write(error.Message);

                    Console.WriteLine();
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }
        finally
        {
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Conveneince method to convert from JsonException to AppValidationError
    /// </summary>
    public static AppValidationError FromJsonError(string filePath, JsonException e)
    {
        return new()
        {
            Message = e.Message,
            ErrorLocation = new FileLocation
            {
                File = filePath,
                LineNumber = e.LineNumber + 1,
                BytePositionInLine = e.BytePositionInLine,
                Path = e.Path,
            }
        };
    }

    /// <summary>
    /// Conveniecne methtod to get the errro localtion from a component (loads and parses json)
    /// </summary>
    public static IErrorLocation? FromJsonComponent(LayoutSet? set, BaseComponent component, AppFilesBytes appFilesBytes)
    {
        try
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };
            LayoutErrorFinder.SetComponent(component);
            JsonSerializer.Deserialize<LayoutErrorFinder>(appFilesBytes.LayoutSetFiles[set?.Id ?? string.Empty].Pages[component.PageId], options);
        }
        catch (JsonException e)
        {
            return new FileLocation
            {
                File = Path.Join("App", "ui", set?.Id, "layouts", component.PageId + ".json"),
                LineNumber = e.LineNumber + 1,
                BytePositionInLine = e.BytePositionInLine,
                Path = e.Path,
            };
        }
        return null;
    }


    /// <summary>
    /// Human readable message about the error
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Interface for multiple types of location information
    /// </summary>
    public IErrorLocation? ErrorLocation { get; set; }
}

/// <summary>
/// Interface for multiple types of location information
/// </summary>
public interface IErrorLocation
{
    /// <summary>
    /// Print error location to string for console display
    /// </summary>
    public string PrintConsole();
}

/// <summary>
/// Implementation of <see cref="IErrorLocation" /> with <see cref="File" />, <see cref="LineNumber" /> and <see cref="BytePositionInLine" /> that is suitable for JsonError s from parsing
/// </summary>
public class FileLocation : IErrorLocation
{
    /// <summary>
    /// The file that failed validation
    /// </summary>
    public string? File { get; set; }
    /// <summary>
    /// Line number for helping writers find the correct spot in the file to fix an error
    /// </summary>
    public long? LineNumber { get; set; }
    /// <summary>
    /// Line number for helping writers find the correct spot in the file to fix an error
    /// </summary>
    public long? BytePositionInLine { get; set; }
    /// <summary>
    /// JsonPath for the error that occured
    /// </summary>
    public string? Path { get; set; }

    /// <inheritdoc />
    public string PrintConsole()
    {
        return $"{File}:{LineNumber}:{BytePositionInLine}";
    }
}