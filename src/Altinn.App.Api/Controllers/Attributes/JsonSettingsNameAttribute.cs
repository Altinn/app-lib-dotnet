namespace Altinn.App.Api.Controllers.Attributes;

[AttributeUsage(AttributeTargets.Class)]
internal class JsonSettingsNameAttribute : Attribute
{
    internal JsonSettingsNameAttribute(string name)
    {
        Name = name;
    }

    internal string Name { get; }
}
