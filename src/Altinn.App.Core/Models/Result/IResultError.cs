namespace Altinn.App.Core.Models.Result;

/// <summary>
/// Interface for error that can be returned in a result
/// </summary>
public interface IResultError
{
    /// <summary>
    /// The reason for the error
    /// </summary>
    /// <returns></returns>
    public string Reason();
}