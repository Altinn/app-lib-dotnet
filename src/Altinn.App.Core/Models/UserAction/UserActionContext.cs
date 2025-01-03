using Altinn.App.Core.Features;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Models.UserAction;

/// <summary>
/// Context for user actions
/// </summary>
public class UserActionContext
{
    /// <summary>
    /// Creates a new instance of the <see cref="UserActionContext"/> class
    /// </summary>
    /// <param name="dataMutator">The instance the action is performed on</param>
    /// <param name="userId">The user performing the action</param>
    /// <param name="buttonId">The id of the button that triggered the action (optional)</param>
    /// <param name="actionMetadata"></param>
    /// <param name="language">The currently used language by the user (or null if not available)</param>
    public UserActionContext(
        IInstanceDataMutator dataMutator,
        int? userId,
        string? buttonId = null,
        Dictionary<string, string>? actionMetadata = null,
        string? language = null
    )
    {
        Instance = dataMutator.Instance;
        DataMutator = dataMutator;
        UserId = userId;
        ButtonId = buttonId;
        ActionMetadata = actionMetadata ?? [];
        Language = language;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="UserActionContext"/> class
    /// </summary>
    /// <param name="instance">The instance the action is performed on</param>
    /// <param name="userId">The user performing the action</param>
    /// <param name="buttonId">The id of the button that triggered the action (optional)</param>
    /// <param name="actionMetadata"></param>
    /// <param name="language">The currently used language by the user (or null if not available)</param>
    [Obsolete("Use the constructor with IInstanceDataAccessor instead")]
    public UserActionContext(
        Instance instance,
        int? userId,
        string? buttonId = null,
        Dictionary<string, string>? actionMetadata = null,
        string? language = null
    )
    {
        Instance = instance;
        // ! TODO: Deprecated constructor, remove in v9
        DataMutator = null!;
        UserId = userId;
        ButtonId = buttonId;
        ActionMetadata = actionMetadata ?? [];
        Language = language;
    }

    /// <summary>
    /// The instance the action is performed on
    /// </summary>
    public Instance Instance { get; }

    /// <summary>
    /// Access dataElements through this accessor to ensure that changes gets saved in storage and returned to frontend
    /// </summary>
    public IInstanceDataMutator DataMutator { get; }

    /// <summary>
    /// The user performing the action
    /// </summary>
    public int? UserId { get; }

    /// <summary>
    /// The id of the button that triggered the action (optional)
    /// </summary>
    public string? ButtonId { get; }

    /// <summary>
    /// Additional metadata for the action
    /// </summary>
    public Dictionary<string, string> ActionMetadata { get; }

    /// <summary>
    /// The language that will be used for the action
    /// </summary>
    public string? Language { get; }
}
