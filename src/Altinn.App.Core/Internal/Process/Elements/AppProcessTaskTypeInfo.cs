#nullable enable
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Altinn.App.Core.Internal.Process.Elements;

/// <summary>
/// Representation of a task's id and type. Used by the frontend to determine which tasks
/// exist, and their type. 
/// </summary>
public class AppProcessTaskTypeInfo
{
    /// <summary>
    /// Default constructor
    /// </summary>
    public AppProcessTaskTypeInfo()
    {
    }

    /// <summary>
    /// Gets or sets the task type
    /// </summary>
    [XmlElement("altinnTaskType", Namespace = "http://altinn.no/process")]
    public string? AltinnTaskType { get; set; }


    /// <summary>
    /// Gets or sets a reference to the current task/event element id as given in the process definition.
    /// </summary>
    [JsonProperty(PropertyName = "elementId")]
    public string? ElementId { get; set; }
}
