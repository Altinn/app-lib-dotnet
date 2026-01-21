using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.ProcessEngine;
using Altinn.App.Core.Internal.Texts;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Data;

/// <summary>
/// Service for initializing an <see cref="InstanceDataUnitOfWork"/> with all the services it needs.
/// </summary>
internal class InstanceDataUnitOfWorkInitializer
{
    private readonly IDataClient _dataClient;
    private readonly IInstanceClient _instanceClient;
    private readonly ITranslationService _translationService;
    private readonly ModelSerializationService _modelSerializationService;
    private readonly IAppResources _appResources;
    private readonly IOptions<FrontEndSettings> _frontEndSettings;
    private readonly Telemetry? _telemetry;
    private readonly IAppMetadata _applicationMetadata;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Constructor with services from dependency injection
    /// </summary>
    public InstanceDataUnitOfWorkInitializer(
        IDataClient dataClient,
        IInstanceClient instanceClient,
        IAppMetadata applicationMetadata,
        ITranslationService translationService,
        ModelSerializationService modelSerializationService,
        IAppResources appResources,
        IOptions<FrontEndSettings> frontEndSettings,
        IServiceProvider serviceProvider,
        Telemetry? telemetry = null
    )
    {
        _dataClient = dataClient;
        _instanceClient = instanceClient;
        _translationService = translationService;
        _modelSerializationService = modelSerializationService;
        _appResources = appResources;
        _frontEndSettings = frontEndSettings;
        _telemetry = telemetry;
        _applicationMetadata = applicationMetadata;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Initializes an <see cref="InstanceDataUnitOfWork"/> with all the services it needs.
    /// This is marked as internal so that this class can only be used internally. Even if it is public for usage (as a DI service) in public classes.
    /// </summary>
    internal async Task<InstanceDataUnitOfWork> Init(Instance instance, string? taskId, string? language)
    {
        var applicationMetadata = await _applicationMetadata.GetApplicationMetadata();
        return new InstanceDataUnitOfWork(
            instance,
            _dataClient,
            _instanceClient,
            applicationMetadata,
            _translationService,
            _modelSerializationService,
            _appResources,
            _frontEndSettings,
            taskId,
            language,
            _telemetry
        );
    }

    /// <summary>
    /// Initializes an <see cref="InstanceDataUnitOfWork"/> and configures a single authentication method
    /// to be used for all data types when communicating with Storage.
    /// </summary>
    /// <param name="instance">The instance to work on.</param>
    /// <param name="taskId">The task context, if any.</param>
    /// <param name="language">The preferred language, if any.</param>
    /// <param name="authenticationMethodForAllDataTypes">
    /// The authentication method to use for all data types, or <c>null</c> to keep the default behaviour.
    /// </param>
    internal async Task<InstanceDataUnitOfWork> Init(
        Instance instance,
        string? taskId,
        string? language,
        StorageAuthenticationMethod? authenticationMethodForAllDataTypes
    )
    {
        var uow = await Init(instance, taskId, language);

        if (authenticationMethodForAllDataTypes is not null)
        {
            uow.UseAuthenticationForAllDataTypes(authenticationMethodForAllDataTypes);
        }

        return uow;
    }

    /// <summary>
    /// Initializes an <see cref="InstanceDataUnitOfWork"/> with Redis caching for a processing session.
    /// </summary>
    internal async Task<InstanceDataUnitOfWork> InitWithSession(
        AppIdentifier appId,
        InstanceIdentifier instanceId,
        string lockToken,
        string? taskId,
        string? language,
        StorageAuthenticationMethod? authenticationMethod,
        CancellationToken ct = default
    )
    {
        var cache = _serviceProvider.GetService<IProcessingSessionCache>() ?? NullProcessingSessionCache.Instance;

        // Try to get Instance from Redis, fall back to Storage
        Instance? instance = await cache.GetInstance(lockToken, ct);

        if (instance is null)
        {
            instance = await _instanceClient.GetInstance(
                appId.App,
                appId.Org,
                instanceId.InstanceOwnerPartyId,
                instanceId.InstanceGuid,
                authenticationMethod,
                ct
            );

            // Cache for subsequent requests
            await cache.SetInstance(lockToken, instance, ct);
        }

        var applicationMetadata = await _applicationMetadata.GetApplicationMetadata();

        // Use instance's current task if taskId not explicitly provided
        var effectiveTaskId = taskId ?? instance.Process?.CurrentTask?.ElementId;

        var uow = new InstanceDataUnitOfWork(
            instance,
            _dataClient,
            _instanceClient,
            applicationMetadata,
            _translationService,
            _modelSerializationService,
            _appResources,
            _frontEndSettings,
            effectiveTaskId,
            language,
            _telemetry,
            lockToken,
            cache
        );

        if (authenticationMethod is not null)
        {
            uow.UseAuthenticationForAllDataTypes(authenticationMethod);
        }

        return uow;
    }
}
