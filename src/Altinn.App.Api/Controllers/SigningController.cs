using Altinn.App.Api.Infrastructure.Filters;
using Altinn.App.Api.Models;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Features.Signing.Models;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Mvc;
using static Altinn.App.Core.Features.Signing.Models.Signee;
using SigneeState = Altinn.App.Api.Models.SigneeState;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Controller for handling signing operations.
/// </summary>
[AutoValidateAntiforgeryTokenIfAuthCookie]
[ApiController]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/signing")]
public class SigningController : ControllerBase
{
    private readonly IInstanceClient _instanceClient;
    private readonly IAppMetadata _appMetadata;
    private readonly IDataClient _dataClient;
    private readonly ModelSerializationService _modelSerialization;
    private readonly IProcessReader _processReader;
    private readonly ILogger<SigningController> _logger;
    private readonly ISigningService _signingService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SigningController"/> class.
    /// </summary>
    public SigningController(
        IServiceProvider serviceProvider,
        IInstanceClient instanceClient,
        IAppMetadata appMetadata,
        IDataClient dataClient,
        ModelSerializationService modelSerialization,
        IProcessReader processReader,
        ILogger<SigningController> logger
    )
    {
        _instanceClient = instanceClient;
        _appMetadata = appMetadata;
        _dataClient = dataClient;
        _modelSerialization = modelSerialization;
        _processReader = processReader;
        _logger = logger;
        _signingService = serviceProvider.GetRequiredService<ISigningService>();
    }

    /// <summary>
    /// Get updated signing state for the current signing task.
    /// </summary>
    /// <param name="org">unique identifier of the organisation responsible for the app</param>
    /// <param name="app">application identifier which is unique within an organisation</param>
    /// <param name="instanceOwnerPartyId">unique id of the party that this the owner of the instance</param>
    /// <param name="instanceGuid">unique id to identify the instance</param>
    /// <param name="language">The currently used language by the user (or null if not available)</param>
    /// <returns>An object containing updated signee state</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SigningStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSigneesState(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromQuery] string? language = null
    )
    {
        Instance instance = await _instanceClient.GetInstance(app, org, instanceOwnerPartyId, instanceGuid);
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();

        _logger.LogInformation(
            "Getting signees state for org {Org} with instance {InstanceGuid} of app {App} for party {PartyId}",
            org,
            instanceGuid,
            app,
            instanceOwnerPartyId
        );

        var cachedDataMutator = new InstanceDataUnitOfWork(
            instance,
            _dataClient,
            _instanceClient,
            appMetadata,
            _modelSerialization
        );

        if (instance.Process.CurrentTask.AltinnTaskType != "signing")
        {
            return NotSigningTask();
        }

        AltinnSignatureConfiguration signingConfiguration =
            (_processReader.GetAltinnTaskExtension(instance.Process.CurrentTask.ElementId)?.SignatureConfiguration)
            ?? throw new ApplicationConfigException("Signing configuration not found in AltinnTaskExtension");

        List<SigneeContext> signeeContexts = await _signingService.GetSigneeContexts(
            cachedDataMutator,
            signingConfiguration
        );

        var response = new SigningStateResponse
        {
            SigneeStates =
            [
                .. signeeContexts
                    .Select(signeeContext =>
                    {
                        string? name = null;
                        string? organisation = null;

                        switch (signeeContext.Signee)
                        {
                            case PersonSignee personSignee:
                                name = personSignee.FullName;
                                break;

                            case PersonOnBehalfOfOrgSignee personOnBehalfOfOrgSignee:
                                name = personOnBehalfOfOrgSignee.FullName;
                                organisation = personOnBehalfOfOrgSignee.OnBehalfOfOrg.OrgName;
                                break;

                            case OrganisationSignee organisationSignee:
                                name = null;
                                organisation = organisationSignee.OrgName;
                                break;

                            case SystemSignee systemSignee:
                                name = "System";
                                organisation = systemSignee.OnBehalfOfOrg.OrgName;
                                break;
                        }

                        return new SigneeState
                        {
                            Name = name,
                            Organisation = organisation,
                            HasSigned = signeeContext.SignDocument is not null,
                            DelegationSuccessful = signeeContext.SigneeState.IsAccessDelegated,
                            NotificationSuccessful = GetNotificationState(signeeContext),
                            PartyId = signeeContext.Signee.GetParty().PartyId,
                        };
                    })
                    .WhereNotNull()
                    .ToList(),
            ],
        };

        return Ok(response);
    }

    /// <summary>
    /// Get the data elements being signed in the current signature task.
    /// </summary>
    /// <param name="org"></param>
    /// <param name="app"></param>
    /// <param name="instanceOwnerPartyId"></param>
    /// <param name="instanceGuid"></param>
    /// <param name="language"></param>
    /// <returns>An object containing the documents to be signed</returns>
    /// <exception cref="ApplicationConfigException"></exception>
    [HttpGet("data-elements")]
    [ProducesResponseType(typeof(SigningDataElementsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDataElements(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromQuery] string? language = null
    )
    {
        Instance instance = await _instanceClient.GetInstance(app, org, instanceOwnerPartyId, instanceGuid);

        if (instance.Process.CurrentTask.AltinnTaskType != "signing")
        {
            return NotSigningTask();
        }

        AltinnSignatureConfiguration? signingConfiguration =
            (_processReader.GetAltinnTaskExtension(instance.Process.CurrentTask.ElementId)?.SignatureConfiguration)
            ?? throw new ApplicationConfigException("Signing configuration not found in AltinnTaskExtension");

        List<DataElement> dataElements =
        [
            .. instance.Data.Where(x => signingConfiguration.DataTypesToSign.Contains(x.DataType)),
        ];

        foreach (DataElement dataElement in dataElements)
        {
            SelfLinkHelper.SetDataAppSelfLinks(instanceOwnerPartyId, instanceGuid, dataElement, Request);
        }

        SigningDataElementsResponse response = new() { DataElements = dataElements };

        return Ok(response);
    }

    private BadRequestObjectResult NotSigningTask()
    {
        return BadRequest(
            new ProblemDetails
            {
                Title = "Not a signing task",
                Detail = "This endpoint is only callable while the current task is a signing task.",
                Status = StatusCodes.Status400BadRequest,
            }
        );
    }

    private static NotificationState GetNotificationState(SigneeContext signeeContext)
    {
        var signeeState = signeeContext.SigneeState;
        if (signeeState.IsMessagedForCallToSign)
        {
            return NotificationState.Sent;
        }

        if (
            signeeState.CallToSignFailedReason is not null
        )
        {
            return NotificationState.Failed;
        }

        return NotificationState.NotSent;
    }
}
