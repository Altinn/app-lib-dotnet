using System.CommandLine;
using System.CommandLine.Invocation;
using altinn_app_cli.fev3tov4.LayoutSetRewriter;
using altinn_app_cli.fev3tov4.IndexFileRewriter;
using altinn_app_cli.fev3tov4.LayoutRewriter;
using altinn_app_cli.fev3tov4.SchemaRefRewriter;
using altinn_app_cli.fev3tov4.FooterRewriter;

namespace altinn_app_cli.fev3tov4.FrontendUpgrade;

class FrontendUpgrade
{
    public static Command GetUpgradeCommand()
    {

        var projectFolderOption = new Option<string>(name: "--folder", description: "The project folder to read", getDefaultValue: () => "CurrentDirectory");
        var targetVersionOption = new Option<string>(name: "--target-version", description: "The target version to upgrade to", getDefaultValue: () => "4.0.0-rc1");
        var indexFileOption = new Option<string>(name: "--index-file", description: "The name of the Index.cshtml file relative to --folder", getDefaultValue: () => "App/views/Home/Index.cshtml");
        var skipIndexFileUpgradeOption = new Option<bool>(name: "--skip-index-file-upgrade", description: "Skip Index.cshtml upgrade", getDefaultValue: () => false);
        var uiFolderOption = new Option<string>(name: "--ui-folder", description: "The folder containing layout files relative to --folder", getDefaultValue: () => "App/ui/");
        var textsFolderOption = new Option<string>(name: "--texts-folder", description: "The folder containing text files relative to --folder", getDefaultValue: () => "App/config/texts/");
        var layoutSetNameOption = new Option<string>(name: "--layout-set-name", description: "The name of the layout set to be created", getDefaultValue: () => "form");
        var applicationMetadataFileOption = new Option<string>(name: "--application-metadata", description: "The path of the applicationmetadata.json file relative to --folder", getDefaultValue: () => "App/config/applicationmetadata.json");
        var skipLayoutSetUpgradeOption = new Option<bool>(name: "--skip-layout-set-upgrade", description: "Skip layout set upgrade", getDefaultValue: () => false);
        var skipLayoutUpgradeOption = new Option<bool>(name: "--skip-layout-upgrade", description: "Skip layout files upgrade", getDefaultValue: () => false);
        var preserveDefaultTriggersOption = new Option<bool>(name: "--preserve-default-triggers", description: "Preserve default schema and component validation triggers", getDefaultValue: () => false);
        var convertGroupTitlesOption = new Option<bool>(name: "--convert-group-titles", description: "Convert 'title' in repeating groups to 'summaryTitle'", getDefaultValue: () => false);
        var skipSchemaRefUpgradeOption = new Option<bool>(name: "--skip-schema-ref-upgrade", description: "Skip schema reference upgrade", getDefaultValue: () => false);
        var skipFooterUpgradeOption = new Option<bool>(name: "--skip-footer-upgrade", description: "Skip footer upgrade", getDefaultValue: () => false);

        var upgradeCommand = new Command("frontend-upgrade", "Upgrade an app from using App-Frontend v3 to v4")
        {
            projectFolderOption,
            targetVersionOption,
            indexFileOption,
            skipIndexFileUpgradeOption,
            uiFolderOption,
            textsFolderOption,
            layoutSetNameOption,
            applicationMetadataFileOption,
            skipLayoutSetUpgradeOption,
            skipLayoutUpgradeOption,
            preserveDefaultTriggersOption,
            convertGroupTitlesOption,
            skipSchemaRefUpgradeOption,
            skipFooterUpgradeOption,
        };

        upgradeCommand.SetHandler(
            async (InvocationContext context) =>
            {
                var returnCode = 0;

                // Get simple options
                var skipIndexFileUpgrade = context.ParseResult.GetValueForOption(skipIndexFileUpgradeOption)!;
                var skipLayoutSetUpgrade = context.ParseResult.GetValueForOption(skipLayoutSetUpgradeOption)!;
                var skipLayoutUpgrade = context.ParseResult.GetValueForOption(skipLayoutUpgradeOption)!;
                var skipSchemaRefUpgrade = context.ParseResult.GetValueForOption(skipSchemaRefUpgradeOption)!;
                var skipFooterUpgrade = context.ParseResult.GetValueForOption(skipFooterUpgradeOption)!;
                var layoutSetName = context.ParseResult.GetValueForOption(layoutSetNameOption)!;
                var preserveDefaultTriggers = context.ParseResult.GetValueForOption(preserveDefaultTriggersOption)!;
                var convertGroupTitles = context.ParseResult.GetValueForOption(convertGroupTitlesOption)!;
                var targetVersion = context.ParseResult.GetValueForOption(targetVersionOption)!;

                var projectFolder = context.ParseResult.GetValueForOption(projectFolderOption)!;
                if (projectFolder == "CurrentDirectory")
                {
                    projectFolder = Directory.GetCurrentDirectory();
                }
                if (File.Exists(projectFolder))
                {
                    Console.WriteLine($"Project folder {projectFolder} does not exist. Please supply location of project with --folder [path/to/project]");
                    returnCode = 1;
                    return;
                }

                // Get options requiring project folder
                var applicationMetadataFile = context.ParseResult.GetValueForOption(applicationMetadataFileOption)!;
                applicationMetadataFile = Path.Combine(projectFolder, applicationMetadataFile);

                var uiFolder = context.ParseResult.GetValueForOption(uiFolderOption)!;
                uiFolder = Path.Combine(projectFolder, uiFolder);

                var textsFolder = context.ParseResult.GetValueForOption(textsFolderOption)!;
                textsFolder = Path.Combine(projectFolder, textsFolder);

                var indexFile = context.ParseResult.GetValueForOption(indexFileOption)!;
                indexFile = Path.Combine(projectFolder, indexFile);

                if (!skipIndexFileUpgrade && returnCode == 0)
                {
                    returnCode = await IndexFileUpgrade(indexFile, targetVersion);
                }

                if (!skipLayoutSetUpgrade && returnCode == 0)
                {

                    returnCode = await LayoutSetUpgrade(uiFolder, layoutSetName, applicationMetadataFile);
                }

                if (!skipLayoutUpgrade && returnCode == 0)
                {

                    returnCode = await LayoutUpgrade(uiFolder, preserveDefaultTriggers, convertGroupTitles);
                }

                if (!skipFooterUpgrade && returnCode == 0)
                {
                    returnCode = await FooterUpgrade(uiFolder);
                }

                if (!skipSchemaRefUpgrade && returnCode == 0)
                {

                    returnCode = await SchemaRefUpgrade(targetVersion, uiFolder, applicationMetadataFile, textsFolder);
                }
            }
        );

        return upgradeCommand;
    }

    private static async Task<int> IndexFileUpgrade(string indexFile, string targetVersion)
    {
        if (!File.Exists(indexFile))
        {
            Console.WriteLine($"Index.cshtml file {indexFile} does not exist. Please supply location of project with --index-file [path/to/Index.cshtml]");
            return 1;
        }

        var rewriter = new IndexFileUpgrader(indexFile, targetVersion);
        rewriter.Upgrade();
        await rewriter.Write();
        var warnings = rewriter.GetWarnings();
        foreach (var warning in warnings)
        {
            Console.WriteLine(warning);
        }
        Console.WriteLine(warnings.Any() ? "Index.cshtml upgraded with warnings. Review the warnings above." : "Index.cshtml upgraded");
        return 0;
    }

    private static async Task<int> LayoutSetUpgrade(string uiFolder, string layoutSetName, string applicationMetadataFile)
    {

        if (File.Exists(Path.Combine(uiFolder, "layout-sets.json")))
        {
            Console.WriteLine("Project already using layout sets. Skipping layout set upgrade.");
            return 0;
        }

        if (!Directory.Exists(uiFolder))
        {
            Console.WriteLine($"Ui folder {uiFolder} does not exist. Please supply location of project with --ui-folder [path/to/ui/]");
            return 1;
        }

        if (!File.Exists(applicationMetadataFile))
        {
            Console.WriteLine($"Application metadata file {applicationMetadataFile} does not exist. Please supply location of project with --application-metadata [path/to/applicationmetadata.json]");
            return 1;
        }

        var rewriter = new LayoutSetUpgrader(uiFolder, layoutSetName, applicationMetadataFile);
        rewriter.Upgrade();
        await rewriter.Write();
        var warnings = rewriter.GetWarnings();
        foreach (var warning in warnings)
        {
            Console.WriteLine(warning);
        }
        Console.WriteLine(warnings.Any() ? "Layout-sets upgraded with warnings. Review the warnings above." : "Layout sets upgraded");
        return 0;
    }

    private static async Task<int> LayoutUpgrade(string uiFolder, bool preserveDefaultTriggers, bool convertGroupTitles)
    {
        if (!Directory.Exists(uiFolder))
        {
            Console.WriteLine($"Ui folder {uiFolder} does not exist. Please supply location of project with --ui-folder [path/to/ui/]");
            return 1;
        }

        if (!File.Exists(Path.Combine(uiFolder, "layout-sets.json")))
        {
            Console.WriteLine("Converting to layout sets is required before upgrading layouts. Skipping layout upgrade.");
            return 1;
        }


        var rewriter = new LayoutUpgrader(uiFolder, preserveDefaultTriggers, convertGroupTitles);
        rewriter.Upgrade();
        await rewriter.Write();
        var warnings = rewriter.GetWarnings();
        foreach (var warning in warnings)
        {
            Console.WriteLine(warning);
        }
        Console.WriteLine(warnings.Any() ? "Layout files upgraded with warnings. Review the warnings above." : "Layout files upgraded");
        return 0;
    }

    private static async Task<int> FooterUpgrade(string uiFolder)
    {
        if (!Directory.Exists(uiFolder))
        {
            Console.WriteLine($"Ui folder {uiFolder} does not exist. Please supply location of project with --ui-folder [path/to/ui/]");
            return 1;
        }

        var rewriter = new FooterUpgrader(uiFolder);
        rewriter.Upgrade();
        await rewriter.Write();
        var warnings = rewriter.GetWarnings();
        foreach (var warning in warnings)
        {
            Console.WriteLine(warning);
        }
        Console.WriteLine(warnings.Any() ? "Footer upgraded with warnings. Review the warnings above." : "Footer upgraded");
        return 0;
    }

    private static async Task<int> SchemaRefUpgrade(string targetVersion, string uiFolder, string applicationMetadataFile, string textsFolder) {
        if (!Directory.Exists(uiFolder))
        {
            Console.WriteLine($"Ui folder {uiFolder} does not exist. Please supply location of project with --ui-folder [path/to/ui/]");
            return 1;
        }

        if (!Directory.Exists(textsFolder))
        {
            Console.WriteLine($"Texts folder {textsFolder} does not exist. Please supply location of project with --texts-folder [path/to/texts/]");
            return 1;
        }

        if (!File.Exists(Path.Combine(uiFolder, "layout-sets.json")))
        {
            Console.WriteLine("Converting to layout sets is required before upgrading schema refereces. Skipping schema reference upgrade.");
            return 1;
        }

        if (!File.Exists(applicationMetadataFile))
        {
            Console.WriteLine($"Application metadata file {applicationMetadataFile} does not exist. Please supply location of project with --application-metadata [path/to/applicationmetadata.json]");
            return 1;
        }

        var rewriter = new SchemaRefUpgrader(targetVersion, uiFolder, applicationMetadataFile, textsFolder);
        rewriter.Upgrade();
        await rewriter.Write();
        var warnings = rewriter.GetWarnings();
        foreach (var warning in warnings)
        {
            Console.WriteLine(warning);
        }
        Console.WriteLine(warnings.Any() ? "Schema references upgraded with warnings. Review the warnings above." : "Schema references upgraded");
        return 0;
    }
}
