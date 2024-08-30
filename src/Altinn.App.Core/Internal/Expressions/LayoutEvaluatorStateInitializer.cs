using System.Diagnostics;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Features;
using Altinn.App.Core.Helpers.DataModel;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.Expressions;

/// <summary>
/// Utility class for collecting all the services from DI that are needed to initialize <see cref="LayoutEvaluatorState" />
/// </summary>
public class LayoutEvaluatorStateInitializer : ILayoutEvaluatorStateInitializer
{
    // Dependency injection properties (set in ctor)
    private readonly IAppResources _appResources;
    private readonly FrontEndSettings _frontEndSettings;

    /// <summary>
    /// Constructor with services from dependency injection
    /// </summary>
    public LayoutEvaluatorStateInitializer(IAppResources appResources, IOptions<FrontEndSettings> frontEndSettings)
    {
        _appResources = appResources;
        _frontEndSettings = frontEndSettings.Value;
    }

    /// <summary>
    /// Helper class to keep compatibility with old interface
    /// delete when <see cref="LayoutEvaluatorStateInitializer.Init(Altinn.Platform.Storage.Interface.Models.Instance,object,string?,string?)"/>
    /// is removed
    /// </summary>
    private class SingleDataElementAccessor : IInstanceDataAccessor
    {
        private readonly DataElement _dataElement;
        private readonly object _data;

        public SingleDataElementAccessor(Instance instance, DataElement dataElement, object data)
        {
            Instance = instance;
            _dataElement = dataElement;
            _data = data;
        }

        public Instance Instance { get; }

        public Task<object> GetData(DataElementId dataElementId)
        {
            if (dataElementId != _dataElement)
            {
                return Task.FromException<object>(
                    new InvalidOperationException(
                        "Use the new ILayoutEvaluatorStateInitializer interface to support multiple data models and subforms"
                    )
                );
            }
            return Task.FromResult(_data);
        }

        public Task<object?> GetSingleDataByType(string dataType)
        {
            if (_dataElement.DataType != dataType)
            {
                return Task.FromException<object?>(
                    new InvalidOperationException("Data type does not match the data element")
                );
            }
            return Task.FromResult<object?>(_data);
        }
    }

    /// <summary>
    /// Initialize LayoutEvaluatorState with given Instance, data object and layoutSetId
    /// </summary>
    [Obsolete("Use the overload with ILayoutEvaluatorStateInitializer instead")]
    public Task<LayoutEvaluatorState> Init(
        Instance instance,
        object data,
        string? layoutSetId,
        string? gatewayAction = null
    )
    {
        var layouts = _appResources.GetLayoutModel(layoutSetId);
        var dataElement = instance.Data.Find(d => d.DataType == layouts.DefaultDataType.Id);
        Debug.Assert(dataElement is not null);
        var dataAccessor = new SingleDataElementAccessor(instance, dataElement, data);
        return Task.FromResult(
            new LayoutEvaluatorState(new DataModel(dataAccessor), layouts, _frontEndSettings, instance, gatewayAction)
        );
    }

    /// <inheritdoc />
    public async Task<LayoutEvaluatorState> Init(
        Instance instance,
        IInstanceDataAccessor dataAccessor,
        string taskId,
        string? gatewayAction = null,
        string? language = null
    )
    {
        var layouts = _appResources.GetLayoutModelForTask(taskId);

        return new LayoutEvaluatorState(
            new DataModel(dataAccessor),
            layouts,
            _frontEndSettings,
            instance,
            gatewayAction,
            language
        );
    }
}
