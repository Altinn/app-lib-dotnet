using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Interface;

public interface IEFormidlingService
{
    public Task SendEFormidlingShipment(Instance instance);
}