using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.App.Core.Models.Layout.Components.Base;

namespace Altinn.App.Core.Models.Layout.Components;

/// <summary>
/// Tag component to signify that this is a grid component
/// </summary>
public sealed class GridComponent : SimpleReferenceComponent
{
    /// <summary>
    /// Constructor for GridComponent
    /// </summary>
    public GridComponent(JsonElement componentElement, string pageId, string layoutId)
        : base(componentElement, pageId, layoutId)
    {
        if (!componentElement.TryGetProperty("rows", out JsonElement rowsElement))
        {
            throw new JsonException("GridComponent must have a \"rows\" property.");
        }

        Rows =
            rowsElement.Deserialize<List<GridRowConfig>>()
            ?? throw new JsonException("Failed to deserialize rows in GridComponent.");

        // Extract component IDs from the Components in the grid so that they can be claimed and used
        // when getting contexts.
        ChildReferences = Rows.SelectMany(r => r.Cells).Select(c => c?.ComponentId).WhereNotNull().ToList();
    }

    /// <summary>
    /// Content from the "rows" property in the JSON representation of the grid component.
    /// </summary>
    public List<GridRowConfig> Rows { get; }

    /// <inheritdoc />
    public override IReadOnlyCollection<string> ChildReferences { get; }

    /// <summary>
    /// Class for parsing a Grid component's rows and cells and extracting the child component IDs
    /// </summary>
    public class GridRowConfig
    {
        /// <summary>
        /// List of cells in the grid row, each cell can optionally refer to a component ID (or static headers we don't care about in backend)
        /// </summary>
        [JsonPropertyName("cells")]
        public required List<GridCellConfig?> Cells { get; set; }
    }

    /// <summary>
    /// Config for a cell in a grid row that refers to a component ID
    /// </summary>
    /// <remarks>
    /// The JSON schema allows different types of cells (polymorphism), but they are only important visually
    /// and can be ignored for backend processing.
    /// </remarks>
    public class GridCellConfig
    {
        /// <summary>
        /// The component ID of the cell
        /// </summary>
        [JsonPropertyName("component")]
        public string? ComponentId { get; set; }
    }
}
