namespace Altinn.App.Api.Tests.Controllers.TestResources;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public class DummyModel
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    public string Name { get; set; }
    public int Age { get; set; }

    /// <summary>
    /// Implement equals for this class
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        DummyModel? dummy = obj as DummyModel;
        if (dummy == null)
        {
            return false;
        }

        return this.Name == dummy.Name && this.Age == dummy.Age;
    }
}