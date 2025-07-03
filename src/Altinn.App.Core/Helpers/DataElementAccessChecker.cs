using Altinn.App.Core.Features.Auth;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Auth;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Core.Helpers;

/// <summary>
/// Helper class for validating if a user is a valid contributor to a data type.
/// </summary>
/// <remarks>
/// The concept of inline authorization of valid contributors is not widely used and is likely not the best approach for doing authorization on the data type level, but there is no support for it yet in the policy based authorization, so keeping for now.
/// </remarks>
internal class DataElementAccessChecker
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAppMetadata _appMetadata;
    private readonly IAuthenticationContext _authenticationContext;

    public DataElementAccessChecker(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor,
        IAuthenticationContext authenticationContext,
        IAppMetadata appMetadata
    )
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
        _authenticationContext = authenticationContext;
        _appMetadata = appMetadata;
    }

    /// <summary>
    /// Checks if the user has access to read a data element of a given data type on an instance.
    /// </summary>
    /// <remarks>The current request <see cref="HttpContext.User"/> is used to determine read access</remarks>
    /// <returns>null for success or ProblemDetails that can be an error response in the Apis</returns>
    public async Task<ProblemDetails?> GetReaderProblem(Instance instance, DataType dataType)
    {
        if (await HasRequiredActionAuthorization(instance, dataType.ActionRequiredToRead) is false)
        {
            return new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = $"Access denied for data element of type {dataType.Id}",
                Status = StatusCodes.Status403Forbidden,
            };
        }

        return null;
    }

    /// <summary>
    /// Checks if the user has access to read a given data element on an instance.
    /// </summary>
    /// <remarks>The current request <see cref="HttpContext.User"/> is used to determine read access</remarks>
    /// <returns>null for success or ProblemDetails that can be an error response in the Apis</returns>
    public async Task<ProblemDetails?> GetReaderProblem(Instance instance, DataElement dataElement)
    {
        ApplicationMetadata appMetadata = await _appMetadata.GetApplicationMetadata();
        DataType dataType = appMetadata.DataTypes.Single(x =>
            x.Id.Equals(dataElement.DataType, StringComparison.OrdinalIgnoreCase)
        );

        return await GetReaderProblem(instance, dataType);
    }

    /// <summary>
    /// Convenience alias for <see cref="GetReaderProblem(Instance,DataType)"/>.
    /// Determines if the current request user can access the given data type
    /// </summary>
    public async Task<bool> CanRead(Instance instance, DataType dataType) =>
        await GetReaderProblem(instance, dataType) is null;

    private async Task<bool> HasRequiredActionAuthorization(Instance instance, string requiredAction)
    {
        if (string.IsNullOrWhiteSpace(requiredAction))
        {
            return true;
        }

        return await _authorizationService.AuthorizeAction(
            new AppIdentifier(instance),
            new InstanceIdentifier(instance),
            _httpContextAccessor.HttpContext?.User ?? throw new InvalidOperationException("No HTTP context available"),
            requiredAction
        );
    }

    // Common checks for create, update and delete
    private async Task<ProblemDetails?> GetMutationProblem(
        Instance instance,
        DataType dataType,
        Authenticated? auth = null
    )
    {
        auth ??= _authenticationContext.Current;

        if (await GetReaderProblem(instance, dataType) is { } readProblem)
        {
            return readProblem;
        }

        // TODO: This compounds the requirements, meaning the user needs both read and write access to mutate. This may or may not be desired...
        if (await HasRequiredActionAuthorization(instance, dataType.ActionRequiredToWrite) is false)
        {
            return new ProblemDetails
            {
                Title = "Unauthorized",
                Detail = $"Access denied for data element of type {dataType.Id}",
                Status = StatusCodes.Status403Forbidden,
            };
        }

        if (!InstanceIsActive(instance))
        {
            return new ProblemDetails
            {
                Title = "Instance Not Active",
                Detail = $"Cannot update data element of archived or deleted instance {instance.Id}",
                Status = StatusCodes.Status409Conflict,
            };
        }

        if (!AllowedContributorsHelper.IsValidContributor(dataType, auth))
        {
            return new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "User is not a valid contributor to the data type",
            };
        }

        return null;
    }

    /// <summary>
    /// Checks if the user has access to create a data element of a given data type on an instance.
    /// </summary>
    /// <returns>null for success or ProblemDetails that can be an error response in the Apis</returns>
    public async Task<ProblemDetails?> GetCreateProblem(
        Instance instance,
        DataType dataType,
        Authenticated? auth = null,
        long? contentLength = null
    )
    {
        auth ??= _authenticationContext.Current;

        // Run the general mutation checks
        if (await GetMutationProblem(instance, dataType, auth) is { } problemDetails)
        {
            return problemDetails;
        }

        // Verify that we don't exceed the max count for data type on the instance
        int existingElements = instance.Data.Count(d => d.DataType == dataType.Id);
        if (dataType.MaxCount > 0 && existingElements >= dataType.MaxCount)
        {
            return new ProblemDetails
            {
                Title = "Max Count Exceeded",
                Detail = $"Cannot create more than {dataType.MaxCount} data elements of type {dataType.Id}",
                Status = StatusCodes.Status409Conflict,
            };
        }

        // Verify that we don't exceed the max size
        if (contentLength.HasValue && dataType.MaxSize > 0 && contentLength > dataType.MaxSize)
        {
            return new ProblemDetails
            {
                Title = "Max Size Exceeded",
                Detail =
                    $"Cannot create data element of size {contentLength} which exceeds the max size of {dataType.MaxSize}",
                Status = StatusCodes.Status400BadRequest,
            };
        }

        // Verify that only orgs can create data elements when DisallowUserCreate is true
        if (dataType.AppLogic?.DisallowUserCreate == true && auth is not Authenticated.ServiceOwner)
        {
            return new ProblemDetails
            {
                Title = "User Create Disallowed",
                Detail = $"Cannot create data element of type {dataType.Id} as it is disallowed by app logic",
                Status = StatusCodes.Status400BadRequest,
            };
        }

        return null;
    }

    /// <summary>
    /// Checks if the user has access to mutate a data element of a given data type on an instance.
    /// </summary>
    /// <returns>null for success or ProblemDetails that can be an error response in the Apis</returns>
    public async Task<ProblemDetails?> GetUpdateProblem(
        Instance instance,
        DataType dataType,
        Authenticated? auth = null
    )
    {
        auth ??= _authenticationContext.Current;

        if (await GetMutationProblem(instance, dataType, auth) is { } problemDetails)
        {
            return problemDetails;
        }

        return null;
    }

    /// <summary>
    /// Checks if the user has access to delete a data element of a given data type on an instance.
    /// </summary>
    /// <returns>null for success or ProblemDetails that can be an error response in the Apis</returns>
    public async Task<ProblemDetails?> GetDeleteProblem(
        Instance instance,
        DataType dataType,
        Guid dataElementId,
        Authenticated? auth = null
    )
    {
        auth ??= _authenticationContext.Current;

        if (await GetMutationProblem(instance, dataType, auth) is { } problemDetails)
        {
            return problemDetails;
        }

        // Kept for compatibility with old app logic
        // Not sure why this restriction is required, but keeping for now
        if (dataType is { AppLogic.ClassRef: not null, MaxCount: 1, MinCount: 1 })
        {
            return new ProblemDetails
            {
                Title = "Cannot Delete main data element",
                Detail = "Cannot delete the only data element of a class with app logic",
                Status = StatusCodes.Status400BadRequest,
            };
        }

        if (dataType.AppLogic?.DisallowUserDelete == true && auth is not Authenticated.ServiceOwner)
        {
            return new ProblemDetails
            {
                Title = "User Delete Disallowed",
                Detail = $"Cannot delete data element of type {dataType.Id} as it is disallowed by app logic",
                Status = StatusCodes.Status400BadRequest,
            };
        }

        return null;
    }

    private static bool InstanceIsActive(Instance i)
    {
        return i?.Status?.Archived is null && i?.Status?.SoftDeleted is null && i?.Status?.HardDeleted is null;
    }
}
