using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Models.UserAction;

public class UserActionContext
{
    public UserActionContext(Instance instance)
    {
        Instance = instance;
    }

    public Instance Instance { get; set; }
}