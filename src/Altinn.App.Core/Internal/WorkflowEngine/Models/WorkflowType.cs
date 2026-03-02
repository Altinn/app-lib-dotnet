using System.Text.Json.Serialization;

namespace Altinn.App.Core.Internal.WorkflowEngine.Models;

/// <summary>
/// The type of workflow.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum WorkflowType
{
    /// <summary>A generic workflow.</summary>
    Generic = 0,

    /// <summary>A workflow representing an app process change.</summary>
    AppProcessChange = 1,
}
