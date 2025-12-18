using Altinn.App.ProcessEngine.Models;

namespace Altinn.App.ProcessEngine.Data.Entities;

internal interface IWithStatus
{
    ProcessEngineItemStatus Status { get; set; }
}
