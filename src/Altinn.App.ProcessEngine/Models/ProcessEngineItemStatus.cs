namespace Altinn.App.ProcessEngine.Models;

internal enum ProcessEngineItemStatus
{
    Enqueued = 0,
    Processing = 1,
    Requeued = 2,
    Completed = 3,
    Failed = 4,
    Canceled = 5,
}
