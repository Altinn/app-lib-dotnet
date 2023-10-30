using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace altinn_app_cli.v7Tov8.ProjectRewriters;

public class ProjectFileRewriter
{
    private XDocument doc;
    private readonly string projectFilePath;
    private readonly string targetVersion;

    public ProjectFileRewriter(string projectFilePath, string targetVersion = "8.0.0")
    {
        this.projectFilePath = projectFilePath;
        this.targetVersion = targetVersion;
        var xmlString = File.ReadAllText(projectFilePath);
        doc = XDocument.Parse(xmlString);
    }

    public async Task Upgrade()
    {
        var altinnAppCoreElements = GetAltinnAppCoreElement();
        altinnAppCoreElements?.ForEach(c => c.Attribute("Version")?.SetValue(targetVersion));

        var altinnAppApiElements = GetAltinnAppApiElement();
        altinnAppApiElements?.ForEach(a => a.Attribute("Version")?.SetValue(targetVersion));

        AddIgnoreWarnings("1591", "1998"); // Require xml doc and await in async methods
        EnableImplicitUsings();

        await Save();
    }

    private void AddIgnoreWarnings(params string[] warnings)
    {
        var noWarn = doc.Root?.Elements("PropertyGroup").Elements("NoWarn").ToList();
        switch (noWarn?.Count)
        {
            case 0:
                doc.Root?.Elements("PropertyGroup").First().Add(new XElement("NoWarn", "$(NoWarn);" + string.Join(';', warnings)));
                break;

            case 1:
                var valueElement = noWarn.First();
                foreach (var warning in warnings)
                {
                    if (!valueElement.Value.Contains(warning))
                    {
                        valueElement.SetValue($"{valueElement.Value};{warning}");
                    }
                }

                break;
        }
    }

    private void EnableImplicitUsings()
    {
        if (doc.Root?.Elements("PropertyGroup").Elements("ImplicitUsings").Count() == 0)
        {
            doc.Root!.Elements("PropertyGroup").First().Add(new XElement("ImplicitUsings", "enable"));
        }
    }

    private List<XElement>? GetAltinnAppCoreElement()
    {
        return doc.Root?.Elements("ItemGroup").Elements("PackageReference").Where(x => x.Attribute("Include")?.Value == "Altinn.App.Core").ToList();
    }

    private List<XElement>? GetAltinnAppApiElement()
    {
        return doc.Root?.Elements("ItemGroup").Elements("PackageReference").Where(x => x.Attribute("Include")?.Value == "Altinn.App.Api").ToList();
    }

    private async Task Save()
    {
        XmlWriterSettings xws = new XmlWriterSettings();
        xws.Async = true;
        xws.OmitXmlDeclaration = true;
        xws.Indent = true;
        xws.Encoding = Encoding.UTF8;
        await using XmlWriter xw = XmlWriter.Create(projectFilePath, xws);
        await doc.WriteToAsync(xw, CancellationToken.None);
    }
}
