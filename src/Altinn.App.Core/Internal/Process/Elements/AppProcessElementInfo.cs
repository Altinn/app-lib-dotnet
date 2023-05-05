using System.Text.Json.Serialization;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Internal.Process.Elements;

public class AppProcessElementInfo: ProcessElementInfo
{
    public AppProcessElementInfo()
    {
        Actions = new Dictionary<string, bool>();
    }
    
    public AppProcessElementInfo(ProcessElementInfo processElementInfo)
    {
        Flow = processElementInfo.Flow;
        Started = processElementInfo.Started;
        ElementId = processElementInfo.ElementId;
        Name = processElementInfo.Name;
        AltinnTaskType = processElementInfo.AltinnTaskType;
        Ended = processElementInfo.Ended;
        Validated = processElementInfo.Validated;
        FlowType = processElementInfo.FlowType;
        Actions = new Dictionary<string, bool>();
    }
    /// <summary>
    /// Actions that can be performed and if the user is allowed to perform them.
    /// </summary>
    [JsonPropertyName(name:"actions")]
    public Dictionary<string, bool>? Actions { get; set; }
    
    /// <summary>
    /// Indicates if the user has read access to the task.
    /// </summary>
    [JsonPropertyName(name:"read")]
    public bool HasReadAccess { get; set; }
    
    /// <summary>
    /// Indicates if the user has write access to the task.
    /// </summary>
    [JsonPropertyName(name:"write")]
    public bool HasWriteAccess { get; set; }
}