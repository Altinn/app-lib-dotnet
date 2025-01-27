using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Profile;
using Altinn.App.Core.Internal.Registers;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.UserAction;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Action;

/// <summary>
/// User action for payment
/// </summary>
internal class InitializeDelegatedSigningUserAction : IUserAction
{
    private readonly IProcessReader _processReader;
    private readonly ILogger<InitializeDelegatedSigningUserAction> _logger;
    private readonly ISigningService _signingService;
    private readonly IAppMetadata _appMetadata;
    private readonly IDataClient _dataClient;
    private readonly IInstanceClient _instanceClient;
    private readonly ModelSerializationService _modelSerialization;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserHelper _userHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="InitializeDelegatedSigningUserAction"/> class
    /// </summary>
    public InitializeDelegatedSigningUserAction(
        IProcessReader processReader,
        ISigningService signingService,
        IAppMetadata appMetadata,
        IDataClient dataClient,
        IInstanceClient instanceClient,
        ModelSerializationService modelSerialization,
        IHttpContextAccessor httpContextAccessor,
        IProfileClient profileClient,
        IAltinnPartyClient altinnPartyClientClient,
        IOptions<GeneralSettings> settings,
        ILogger<InitializeDelegatedSigningUserAction> logger
    )
    {
        _processReader = processReader;
        _signingService = signingService;
        _appMetadata = appMetadata;
        _dataClient = dataClient;
        _instanceClient = instanceClient;
        _modelSerialization = modelSerialization;
        _httpContextAccessor = httpContextAccessor;
        _userHelper = new UserHelper(profileClient, altinnPartyClientClient, settings);
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

        AltinnSignatureConfiguration? signatureConfiguration =
            (currentTask.ExtensionElements?.TaskExtension?.SignatureConfiguration)
            ?? throw new ApplicationConfigException(
                "SignatureConfiguration is missing in the payment process task configuration."
            );

        var cachedDataMutator = new InstanceDataUnitOfWork(
            context.Instance,
            _dataClient,
            _instanceClient,
            await _appMetadata.GetApplicationMetadata(),
            _modelSerialization
        );
        CancellationToken ct = new();
        List<SigneeContext> signeeContexts = await _signingService.GenerateSigneeContexts(
            cachedDataMutator,
            signatureConfiguration,
            ct
        );

        UserContext userContext = await _userHelper.GetUserContext(
            _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.")
        );

        await _signingService.InitialiseSignees(
            currentTask.Id,
            userContext.UserParty,
            cachedDataMutator,
            signeeContexts,
            signatureConfiguration,
            ct
        );
        var changes = cachedDataMutator.GetDataElementChanges(false);
        await cachedDataMutator.SaveChanges(changes);

        //TODO: Return failure result if something failed.

        return UserActionResult.SuccessResult();
    }
}
