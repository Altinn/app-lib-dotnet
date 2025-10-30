namespace Altinn.App.ProcessEngine.Models;

/// <summary>
/// The response from the process engine for a <see cref="ProcessEngineRequest"/>.
/// </summary>
public sealed record ProcessEngineResponse(ProcessEngineRequestStatus Status, string? Message = null)
{
    public static ProcessEngineResponse Accepted(string? message = null) =>
        new(ProcessEngineRequestStatus.Accepted, message);

    public static ProcessEngineResponse Rejected(string? message = null) =>
        new(ProcessEngineRequestStatus.Rejected, message);
};
