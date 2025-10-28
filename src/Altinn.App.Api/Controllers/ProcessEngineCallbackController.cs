using Altinn.App.Core.Infrastructure.Clients.Storage;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.ProcessEngine;
using Altinn.App.Core.Models;
using Altinn.App.ProcessEngine.Constants;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessEngineCallbackPayload = Altinn.App.Core.Internal.ProcessEngine.Models.ProcessEngineCallbackPayload;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Controller for handling process engine callbacks.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = AuthConstants.ApiKeySchemeName)]
[Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/process-engine-callback")]
public class ProcessEngineCallbackController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly InstanceClient _instanceClient;
    private readonly InstanceDataUnitOfWorkInitializer _instanceDataUnitOfWorkInitializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessEngineCallbackController"/> class.
    /// </summary>
    public ProcessEngineCallbackController(
        IServiceProvider serviceProvider,
        InstanceClient instanceClient
    )
    {
        _serviceProvider = serviceProvider;
        _instanceClient = instanceClient;
        _instanceDataUnitOfWorkInitializer = serviceProvider.GetRequiredService<InstanceDataUnitOfWorkInitializer>();
    }

    /// <summary>
    /// Executes a command based on the provided command key.
    /// </summary>
    [HttpPost("{commandKey}")]
    public async Task<IActionResult> ExecuteCommand(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromRoute] string commandKey,
        [FromBody] ProcessEngineCallbackPayload payload,
        CancellationToken cancellationToken
    )
    {
        var appId = new AppIdentifier(org, app);
        var instanceId = new InstanceIdentifier(instanceOwnerPartyId, instanceGuid);

        var handler =
            _serviceProvider.GetServices<IProcessEngineCallbackHandler>().FirstOrDefault(x => x.Key == commandKey)
            ?? throw new KeyNotFoundException("yikes");

        Instance instance =
            await _instanceClient.GetInstance(appId.App, appId.Org, instanceOwnerPartyId, instanceId.InstanceGuid);

        InstanceDataUnitOfWork instanceDataUnitOfWork = await _instanceDataUnitOfWorkInitializer.Init(
            instance,
            instance.Process?.CurrentTask?.ElementId,
            payload.ProcessEngineActor.Language
        );

        ProcessEngineCallbackHandlerResult result = await handler.Execute(new ProcessEngineCallbackHandlerParameters
        {
            AppId = appId,
            InstanceId = instanceId,
            InstanceDataMutator = instanceDataUnitOfWork,
            CancellationToken = cancellationToken,
            Payload = payload
        });

        // TODO: Do we have to check for abandon issues here?
        DataElementChanges changes = instanceDataUnitOfWork.GetDataElementChanges(false);
        await instanceDataUnitOfWork.SaveChanges(changes);

        return result is SuccessfulProcessEngineCallbackHandlerResult ? Ok() : BadRequest();
    }
}
