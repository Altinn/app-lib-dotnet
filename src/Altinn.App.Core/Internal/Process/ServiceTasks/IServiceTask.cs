using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.ServiceTasks;

public interface IServiceTask
{
    public Task Execute(string taskId, Instance instance);
}