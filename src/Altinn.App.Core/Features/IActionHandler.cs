using System.Security.Claims;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features;

public interface IActionHandler
{
    string Id { get; }
        
    Task<bool> HandleAction(Instance instance);
}