using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Models.UserAction;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Action;

/// <summary>
/// User action for payment
/// </summary>
internal class InitializeDelegatedSigningUserAction : IUserAction
{
    private readonly IProcessReader _processReader;
    private readonly ILogger<InitializeDelegatedSigningUserAction> _logger;
    private readonly ISigningService _signingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="InitializeDelegatedSigningUserAction"/> class
    /// </summary>
    public InitializeDelegatedSigningUserAction(
        IProcessReader processReader,
        ISigningService signingService,
        ILogger<InitializeDelegatedSigningUserAction> logger
    )
    {
        _processReader = processReader;
        _signingService = signingService;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Id => "initialize-delegated-signing";

    /// <inheritdoc />
    public async Task<UserActionResult> HandleAction(UserActionContext context)
    {
        if (
            _processReader.GetFlowElement(context.Instance.Process.CurrentTask.ElementId) is not ProcessTask currentTask
        )
        {
            return UserActionResult.FailureResult(
                new ActionError() { Code = "NoProcessTask", Message = "Current task is not a process task." }
            );
        }

        _logger.LogInformation(
            "Initialize delegated singing action handler invoked for instance {Id}. In task: {CurrentTaskId}",
            context.Instance.Id,
            currentTask.Id
        );

        List<SigneeContext> signeeContexts = await _signingService.InitializeSignees(currentTask.Id);
        signeeContexts = await _signingService.ProcessSignees(signeeContexts);

        //TODO: Return failure result if something failed.

        return UserActionResult.SuccessResult();
    }
}
