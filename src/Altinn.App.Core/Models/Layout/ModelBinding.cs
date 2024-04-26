namespace Altinn.App.Core.Models.Layout;

public readonly record struct ModelBinding
{
    public ModelBinding()
    {
        DataType = "default";
    }

    public required string Field { get; init; }
    public string DataType { get; init; }

    public static implicit operator ModelBinding(string field)
    {
        return new ModelBinding { Field = field, };
    }
}
