using Altinn.App.Core.Features.Signing;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ProcessTasks;

/// <summary>
/// Represents the process task responsible for signing.
/// </summary>
internal sealed class SigningProcessTask : IProcessTask
{
    private readonly SigningService _signingService;

    public SigningProcessTask(SigningService signingService)
    {
        _signingService = signingService;
    }

    public string Type => "signing";

    /// <inheritdoc/>
    public async Task Start(string taskId, Instance instance)
    {
        await _signingService.InitializeSignees(taskId, new CancellationToken());
    }

    /// <inheritdoc/>
    public async Task End(string taskId, Instance instance)
    {
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Abandon(string taskId, Instance instance)
    {
        await Task.CompletedTask;
    }
}
