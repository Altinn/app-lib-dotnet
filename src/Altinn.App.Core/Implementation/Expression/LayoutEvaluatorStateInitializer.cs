using System.Text.Json;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Configuration;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Implementation.Expression;

public class LayoutEvaluatorStateInitializer
{
    // Dependency injection properties (set in ctor)
    private readonly IData _data;
    private readonly IAppResources _appResources;
    private readonly FrontEndSettings _frontEndSettings;

    public LayoutEvaluatorStateInitializer(IData data, IAppResources appResources, IOptions<FrontEndSettings> frontEndSettings)
    {
        _data = data;
        _appResources = appResources;
        _frontEndSettings = frontEndSettings.Value;
    }

    public async Task<LayoutEvaluatorState> Init(Guid instanceGuid, Type type, string org, string app, int instanceOwnerPartyId, Guid dataId)
    {
        var layoutsString = _appResources.GetLayouts();
        var layouts = JsonSerializer.Deserialize<ComponentModel>(layoutsString);

        //TODO: Only load data that is actually referenced from layout expressions
        Instance? instance = null;
        object data = await _data.GetFormData(instanceGuid, type, org, app, instanceOwnerPartyId, dataId);
        
        return new LayoutEvaluatorState(new DataModel(data), layouts, _frontEndSettings, instance);

    }
}