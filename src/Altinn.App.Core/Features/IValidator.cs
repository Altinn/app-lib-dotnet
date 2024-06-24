using Altinn.App.Core.Models.Validation;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features;

public interface IValidator
{
    public string TaskId { get; }

    public string ValidationSource => $"{GetType().FullName}-{TaskId}";

    public Task<List<ValidationIssue>> Validate(
        Instance instance,
        string taskId,
        string? language,
        IInstanceDataAccessor instanceDataAccessor
    );
}

public class DataElementChange
{
    public DataElement DataElement { get; init; }
    public object PreviousValue { get; init; }
    public object CurrentValue { get; init; }
}

public interface IInstanceDataAccessor
{
    Task<object> Get(Instance instance, DataElement dataElement);
}

public interface IIncrementalValidator : IValidator
{
    public Task<bool> HasRelevantChanges(
        Instance instance,
        string taskId,
        string? language,
        List<DataElementChange> changes,
        IInstanceDataAccessor instanceDataAccessor
    );
}
