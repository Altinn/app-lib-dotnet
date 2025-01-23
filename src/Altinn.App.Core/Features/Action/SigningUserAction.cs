using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Models.Process;
using Altinn.App.Core.Models.UserAction;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Action;

/// <summary>
/// Class handling tasks that should happen when action signing is performed.
/// </summary>
public class SigningUserAction : IUserAction
{
    private readonly IProcessReader _processReader;
    private readonly ILogger<SigningUserAction> _logger;
    private readonly ISigningService _signingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SigningUserAction"/> class
    /// </summary>
    /// <param name="processReader">The process reader</param>
    /// <param name="logger">The logger</param>
    /// <param name="signingService">The signing service</param>
    public SigningUserAction(
        IProcessReader processReader,
        ISigningService signingService,
        ILogger<SigningUserAction> logger
    )
    {
        _processReader = processReader;
        _signingService = signingService;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Id => "sign";

    /// <inheritdoc />
    /// <exception cref="Helpers.PlatformHttpException"></exception>
    /// <exception cref="ApplicationConfigException"></exception>
    public async Task<UserActionResult> HandleAction(UserActionContext context)
    {
        if (context.UserId is null)
        {
            return UserActionResult.FailureResult(
                error: new ActionError { Code = "NoUserId", Message = "User id is missing in token" },
                errorType: ProcessErrorType.Unauthorized
            );
        }

        ProcessTask? currentTask =
            _processReader.GetFlowElement(context.Instance.Process.CurrentTask.ElementId) as ProcessTask;

        if (currentTask is null)
        {
            return UserActionResult.FailureResult(
                new ActionError { Code = "NoProcessTask", Message = "Current task is not a process task." }
            );
        }

        _logger.LogInformation(
            "Signing action handler invoked for instance {Id}. In task: {CurrentTaskId}",
            context.Instance.Id,
            currentTask.Id
        );

        await _signingService.Sign(context, currentTask);

        // TODO: Metrics

        return UserActionResult.SuccessResult();
    }
}
