namespace Altinn.App.Core.Implementation.Expression;

public class ComponentContext
{
    public ComponentContext(Component component, int[]? RowIndicies)
    {
        Component = component;
        RowIndices = RowIndicies;
    }

    public ComponentContext(ComponentModel componentModel, string componentId, string page, int[]? rowIndicies)
    {
        Component = componentModel.GetComponent(page, componentId);
        RowIndices = rowIndicies;
    }

    public Component Component { get; init; }

    public int[]? RowIndices { get; init; }
}