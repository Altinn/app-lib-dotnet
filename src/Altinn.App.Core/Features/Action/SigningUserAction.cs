using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Models.UserAction;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Action;

/// <summary>
/// Class handling tasks that should happen when action signing is performed.
/// </summary>
public class SigningUserAction: IUserAction
{
    private readonly IProcessReader _processReader;
    private readonly ILogger<SigningUserAction> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SigningUserAction"/> class
    /// </summary>
    /// <param name="processReader">The process reader</param>
    /// <param name="logger">The logger</param>
    public SigningUserAction(IProcessReader processReader, ILogger<SigningUserAction> logger)
    {
        _logger = logger;
        _processReader = processReader;
    }
    
    /// <inheritdoc />
    public string Id => "sign";

    /// <inheritdoc />
    public async Task<bool> HandleAction(UserActionContext context)
    {
        await Task.CompletedTask;
        if (_processReader.GetFlowElement(context.Instance.Process.CurrentTask.ElementId) is ProcessTask currentTask)
        {
            _logger.LogInformation("Signing action handler invoked for instance {Id}. In task: {CurrentTaskId}", context.Instance.Id, currentTask.Id);
            //TODO add call to signing method in data service
            return true;
        }

        return false;
    }
}