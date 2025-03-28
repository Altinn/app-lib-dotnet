using Altinn.App.Core.Internal.Process.Elements;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Clients.Fiks.FiksArkiv;

/// <summary>
/// Interface for validating the configuration of the FIKS Arkiv client.
/// </summary>
public interface IFiksArkivConfigValidation
{
    /// <summary>
    /// Validates the configuration of the FIKS Arkiv client.
    /// </summary>
    /// <param name="configuredDataTypes">All datatypes defined in applicationmetadata.json.</param>
    /// <param name="configuredProcessTasks">All process tasks defined in process.bpmn.</param>
    /// <returns></returns>
    Task ValidateConfiguration(
        IReadOnlyList<DataType> configuredDataTypes,
        IReadOnlyList<ProcessTask> configuredProcessTasks
    );
}
