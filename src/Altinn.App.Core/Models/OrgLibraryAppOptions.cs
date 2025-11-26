namespace Altinn.App.Core.Models;

/// <summary>
/// Altinn 3 Library app options
/// </summary>
public class OrgLibraryAppOptions
{
    /// <summary>
    /// List of options
    /// </summary>
    public List<OrgLibraryAppOption> Options { get; set; }

    /// <summary>
    /// Name of the code list
    /// </summary>
    public string Name { get; set; }

    /// Tag names used for grouping in combination with <see cref="Altinn3LibraryCodeListItem.Tags"/>
    /// Eg: tagNames: ["region"], tags: ["europe"]
    public List<string> TagNames { get; set; }
}
