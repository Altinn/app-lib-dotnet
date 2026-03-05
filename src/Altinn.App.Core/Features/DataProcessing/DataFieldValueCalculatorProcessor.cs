using Altinn.App.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.App.Core.Features.DataProcessing;

/// <summary>
/// Processing data fields values that is calculated by expressions provided in [modelName].calculation.json.
/// </summary>
public class DataFieldValueCalculatorProcessor : IDataWriteProcessor
{
    private readonly DataFieldValueCalculator _dataFieldValueCalculator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataFieldValueCalculatorProcessor"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    public DataFieldValueCalculatorProcessor(IServiceProvider serviceProvider)
    {
        _dataFieldValueCalculator = serviceProvider.GetRequiredService<DataFieldValueCalculator>();
    }

    /// <summary>
    /// Processes data write operations on properties in the data model.
    /// </summary>
    /// <param name="instanceDataMutator">Object to fetch data elements not included in changes</param>
    /// <param name="taskId">The current task ID</param>
    /// <param name="changes">Not used in this context</param>
    /// <param name="language">Not used in this context</param>
    public async Task ProcessDataWrite(
        IInstanceDataMutator instanceDataMutator,
        string taskId,
        DataElementChanges changes,
        string? language
    )
    {
        await _dataFieldValueCalculator.Calculate(instanceDataMutator, taskId);
    }
}
