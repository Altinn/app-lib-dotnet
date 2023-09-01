using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Models.Expressions;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// Component specialisation for repeating groups with maxCount > 1
/// </summary>
public class GridComponent : GroupComponent
{
    /// <summary>
    /// Constructor for RepeatingGroupComponent
    /// </summary>
    public GridComponent(string id, string type, IReadOnlyDictionary<string, string>? dataModelBindings, IEnumerable<BaseComponent> children, IEnumerable<string>? childIDs, Expression? hidden, Expression? required, Expression? readOnly, IReadOnlyDictionary<string, string>? additionalProperties) :
        base(id, type, dataModelBindings, children, childIDs, hidden, required, readOnly, additionalProperties)
    { }
}


/// <inheritdoc/>
public class GridConfig
{

    /// <inheritdoc/>
    public static List<string> ReadGridChildren(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var rows = JsonSerializer.Deserialize<GridRow[]>(ref reader, options);
        var gridConfig = new GridConfig { Rows = rows };
        return gridConfig.Children();
    }

    /// <inheritdoc/>
    [JsonPropertyName("rows")]
    public GridRow[]? Rows { get; set; }

    /// <inheritdoc/>
    public class GridRow
    {
        /// <inheritdoc/>
        [JsonPropertyName("cells")]
        public GridCell[]? Cells { get; set; }

        /// <inheritdoc/>
        public class GridCell
        {
            /// <inheritdoc/>
            [JsonPropertyName("component")]
            public string? ComponentId { get; set; }
        }
    }

    /// <inheritdoc/>
    public List<String> Children()
    {
        List<String> gridChildren = new List<String>();

        if (this.Rows is null)
        {
            return gridChildren;
        }

        foreach (var row in this.Rows)
        {
            if (row.Cells is null)
            {
                continue;
            }

            foreach (var cell in row.Cells)
            {
                if (cell.ComponentId is not null)
                {
                    gridChildren.Add(cell.ComponentId);
                }
            }
        }
        return gridChildren;
    }
}
