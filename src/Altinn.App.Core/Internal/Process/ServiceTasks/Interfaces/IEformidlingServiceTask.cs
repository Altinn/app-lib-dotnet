namespace Altinn.App.Core.Internal.Process.ServiceTasks;

/// <summary>
/// Service task that sends eFormidling shipment, if EFormidling is enabled in config and EFormidling.SendAfterTaskId matches the current task.
/// </summary>
internal interface IEformidlingServiceTask : IServiceTask { }
