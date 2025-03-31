using System.Globalization;
using Altinn.App.Core.Exceptions;
using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Features.Correspondence.Models;
using Altinn.App.Core.Features.Signing.Exceptions;
using Altinn.App.Core.Features.Signing.Interfaces;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Internal.Sign;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Process;
using Altinn.App.Core.Models.Result;
using Altinn.App.Core.Models.UserAction;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Signee = Altinn.App.Core.Internal.Sign.Signee;

namespace Altinn.App.Core.Features.Action;

/// <summary>
/// Class handling tasks that should happen when action signing is performed.
/// </summary>
internal class SigningUserAction : IUserAction
{
    private readonly IProcessReader _processReader;
    private readonly IAppMetadata _appMetadata;
    private readonly ISigningReceiptService _signingReceiptService;
    private readonly ISigningService _signingService;
    private readonly ILogger<SigningUserAction> _logger;
    private readonly ISignClient _signClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="SigningUserAction"/> class
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="processReader">The process reader</param>
    /// <param name="signClient">The sign client</param>
    /// <param name="appMetadata">The application metadata</param>
    /// <param name="signingReceiptService">The signing receipt service</param>
    /// <param name="logger">The logger</param>
    public SigningUserAction(
        IServiceProvider serviceProvider,
        IProcessReader processReader,
        ISignClient signClient,
        IAppMetadata appMetadata,
        ISigningReceiptService signingReceiptService,
        ILogger<SigningUserAction> logger
    )
    {
        _processReader = processReader;
        _signClient = signClient;
        _appMetadata = appMetadata;
        _signingReceiptService = signingReceiptService;
        _signingService = serviceProvider.GetRequiredService<ISigningService>();
        _logger = logger;
    }

    /// <inheritdoc />
    public string Id => "sign";

    /// <inheritdoc />
    /// <exception cref="Helpers.PlatformHttpException"></exception>
    /// <exception cref="ApplicationConfigException"></exception>
    public async Task<UserActionResult> HandleAction(UserActionContext context)
    {
        if (
            context.Authentication
            is not Authenticated.User
                and not Authenticated.SelfIdentifiedUser
                and not Authenticated.SystemUser
        )
        {
            return UserActionResult.FailureResult(
                error: new ActionError { Code = "NoUserId", Message = "User id is missing in token" },
                errorType: ProcessErrorType.Unauthorized
            );
        }

        if (
            _processReader.GetFlowElement(context.Instance.Process.CurrentTask.ElementId) is not ProcessTask currentTask
        )
        {
            return UserActionResult.FailureResult(
                new ActionError() { Code = "NoProcessTask", Message = "Current task is not a process task." }
            );
        }

        _logger.LogInformation(
            "Signing action handler invoked for instance {Id}. In task: {CurrentTaskId}",
            context.Instance.Id,
            currentTask.Id
        );

        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        AltinnSignatureConfiguration signatureConfiguration =
            currentTask.ExtensionElements?.TaskExtension?.SignatureConfiguration
            ?? throw new ApplicationConfigException(
                "Missing configuration for signing. Check that the task has a signature configuration and that the data types to sign are defined."
            );
        List<string> dataTypeIds = signatureConfiguration.DataTypesToSign ?? [];
        List<DataType>? dataTypesToSign =
            appMetadata.DataTypes?.Where(d => dataTypeIds.Contains(d.Id, StringComparer.OrdinalIgnoreCase)).ToList()
            ?? throw new ApplicationConfigException(
                "Faulty configuration for signing task. Unable to data types to sign."
            );

        string signatureDataType =
            GetDataTypeForSignature(currentTask, context.Instance.Data, dataTypesToSign)
            ?? throw new ApplicationConfigException(
                "Faulty configuration for signing task. Unable to get data type for signature."
            );

        List<DataElementSignature>? dataElementSignatures = GetDataElementSignatures(
            context.Instance.Data,
            dataTypesToSign
        );
        SignatureContext signatureContext = new(
            new InstanceIdentifier(context.Instance),
            currentTask.Id,
            signatureDataType,
            await GetSignee(context),
            dataElementSignatures
        );

        if (!string.IsNullOrEmpty(context.OnBehalfOf))
        {
            var canSignOnbehalfOf = await HandleOnBehalfOf(context, signatureConfiguration);
            if (!canSignOnbehalfOf)
            {
                return UserActionResult.FailureResult(
                    error: new ActionError()
                    {
                        Code = "UnauthorizedOnBehalfOf",
                        Message = "Unauthorized to sign on behalf of.",
                    },
                    errorType: ProcessErrorType.Unauthorized
                );
            }
        }

        try
        {
            await _signClient.SignDataElements(signatureContext);
        }
        catch (PlatformHttpException)
        {
            return UserActionResult.FailureResult(
                error: new ActionError() { Code = "SignDataElementsFailed", Message = "Failed to sign data elements." },
                errorType: ProcessErrorType.Internal
            );
        }

        ServiceResult<SendCorrespondenceResponse?, Exception> res = await CatchError(
            _signingReceiptService.SendSignatureReceipt(
                signatureContext.InstanceIdentifier,
                signatureContext.Signee,
                dataElementSignatures,
                context,
                signatureConfiguration.CorrespondenceResources
            )
        );

        if (res.Success)
        {
            _logger.LogInformation(
                "Correspondence successfully sent to {Recipients}",
                string.Join(", ", res.Ok.Correspondences.Select(x => x.Recipient))
            );
        }
        else if (res.Error is ConfigurationException configurationException)
        {
            // TODO: What do we do here? Probably nothing.
            _logger.LogError(
                configurationException,
                "Correspondence send failed: {Exception}",
                configurationException.Message
            );
        }
        else
        {
            // TODO: What do we do here? This failure is pretty silent... but throwing would cause havoc
            _logger.LogWarning("Correspondence configuration error: {Exception}", res.Error.Message);
        }

        return UserActionResult.SuccessResult();
    }

    internal async Task<bool> HandleOnBehalfOf(
        UserActionContext context,
        AltinnSignatureConfiguration signatureConfiguration
    )
    {
        if (context.OnBehalfOf == context.Instance.InstanceOwner.OrganisationNumber)
        {
            _logger.LogInformation("On behalf of the instance owner, no need to check for authorized organisations.");
            return true;
        }

        int? userId = context.Authentication switch
        {
            Authenticated.User user => user.UserId,
            Authenticated.SelfIdentifiedUser selfIdentifiedUser => selfIdentifiedUser.UserId,
            _ => null,
        };

        if (userId is null)
        {
            _logger.LogWarning(
                "Unsupported authentication type for signing on behalf of {OrganisationNumber}",
                context.OnBehalfOf
            );
            return false;
        }

        // Fetch authorized organisation signees for the extracted user ID
        var authorizedOrganisations = await _signingService.GetAuthorizedOrganisationSignees(
            context.DataMutator,
            signatureConfiguration,
            userId.Value
        );

        bool isAuthorized = authorizedOrganisations.Any(o => o.OrgNumber == context.OnBehalfOf);

        if (isAuthorized)
        {
            _logger.LogInformation("User is authorized to sign on behalf of {OrganisationNumber}", context.OnBehalfOf);
        }
        else
        {
            _logger.LogWarning("User is not authorized to sign on behalf of {OrganisationNumber}", context.OnBehalfOf);
        }

        return isAuthorized;
    }

    private static string? GetDataTypeForSignature(
        ProcessTask currentTask,
        List<DataElement> dataElements,
        List<DataType>? dataTypesToSign
    )
    {
        var signatureDataType = currentTask.ExtensionElements?.TaskExtension?.SignatureConfiguration?.SignatureDataType;
        if (dataTypesToSign is null or [] || signatureDataType is null)
        {
            return null;
        }

        var dataElementMatchExists = dataElements.Any(de =>
            dataTypesToSign.Any(dt => string.Equals(dt.Id, de.DataType, StringComparison.OrdinalIgnoreCase))
        );
        var allDataTypesAreOptional = dataTypesToSign.All(d => d.MinCount == 0);
        return dataElementMatchExists || allDataTypesAreOptional ? signatureDataType : null;
    }

    private static List<DataElementSignature> GetDataElementSignatures(
        List<DataElement> dataElements,
        List<DataType> dataTypesToSign
    )
    {
        var connectedDataElements = new List<DataElementSignature>();
        if (dataTypesToSign is null or [])
            return connectedDataElements;
        foreach (var dataType in dataTypesToSign)
        {
            connectedDataElements.AddRange(
                dataElements
                    .Where(d => d.DataType.Equals(dataType.Id, StringComparison.OrdinalIgnoreCase))
                    .Select(d => new DataElementSignature(d.Id))
            );
        }

        return connectedDataElements;
    }

    private static async Task<Signee> GetSignee(UserActionContext context)
    {
        switch (context.Authentication)
        {
            case Authenticated.User user:
            {
                UserProfile userProfile = await user.LookupProfile();
                return new Signee
                {
                    UserId = userProfile.UserId.ToString(CultureInfo.InvariantCulture),
                    PersonNumber = userProfile.Party.SSN,
                    OrganisationNumber = context.OnBehalfOf,
                };
            }
            case Authenticated.SelfIdentifiedUser selfIdentifiedUser:
                return new Signee { UserId = selfIdentifiedUser.UserId.ToString(CultureInfo.InvariantCulture) };
            case Authenticated.SystemUser systemUser:
                return new Signee
                {
                    SystemUserId = systemUser.SystemUserId[0],
                    OrganisationNumber = context.OnBehalfOf,
                };
            default:
                throw new SigningException("Could not get signee");
        }
    }

    /// <summary>
    /// Catch exceptions from a task and return them as a ServiceResult record with the result.
    /// </summary>
    private static async Task<ServiceResult<T, Exception>> CatchError<T>(Task<T> task)
    {
        try
        {
            var result = await task;
            return result;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
