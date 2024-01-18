using System.CommandLine;
using System.CommandLine.Invocation;
using altinn_app_cli.fev3tov4.LayoutSetRewriter;

namespace altinn_app_cli.fev3tov4.FrontendUpgrade;

class FrontendUpgrade
{
    public static Command GetUpgradeCommand()
    {

        var projectFolderOption = new Option<string>(name: "--folder", description: "The project folder to read", getDefaultValue: () => "CurrentDirectory");
        var uiFolderOption = new Option<string>(name: "--ui-folder", description: "The folder containing layout files relative to --folder", getDefaultValue: () => "App/ui/");
        var applicationMetadataFileOption = new Option<string>(name: "--application-metadata", description: "The path of the applicationmetadata.json file relative to --folder", getDefaultValue: () => "App/config/applicationmetadata.json");
        var skipLayoutSetUpgradeOption = new Option<bool>(name: "--skip-layout-set-upgrade", description: "Skip layout set upgrade", getDefaultValue: () => false);
        var layoutSetNameOption = new Option<string>(name: "--layout-set-name", description: "The name of the layout set to be created", getDefaultValue: () => "form");

        var upgradeCommand = new Command("frontend-upgrade", "Upgrade an app from using App-Frontend v3 to v4")
        {
            projectFolderOption,
            uiFolderOption,
            skipLayoutSetUpgradeOption,
            layoutSetNameOption,
            applicationMetadataFileOption
        };

        upgradeCommand.SetHandler(
            async (InvocationContext context) =>
            {
                var returnCode = 0;

                var skipLayoutSetUpgrade = context.ParseResult.GetValueForOption(skipLayoutSetUpgradeOption)!;
                var layoutSetName = context.ParseResult.GetValueForOption(layoutSetNameOption)!;

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

                var applicationMetadataFile = context.ParseResult.GetValueForOption(applicationMetadataFileOption)!;
                applicationMetadataFile = Path.Combine(projectFolder, applicationMetadataFile);
                if (!File.Exists(applicationMetadataFile))
                {
                    Console.WriteLine($"Application metadata file {applicationMetadataFile} does not exist. Please supply location of project with --folder [path/to/applicationmetadata.json]");
                    returnCode = 1;
                    return;
                }

                var uiFolder = context.ParseResult.GetValueForOption(uiFolderOption)!;
                uiFolder = Path.Combine(projectFolder, uiFolder);

                if (!skipLayoutSetUpgrade && returnCode == 0)
                {

                    returnCode = await LayoutSetUpgrade(uiFolder, layoutSetName, applicationMetadataFile);
                }
            }
        );

        return upgradeCommand;
    }

    private static async Task<int> LayoutSetUpgrade(string uiFolder, string layoutSetName, string applicationMetadataFile)
    {
        if (!Directory.Exists(uiFolder))
        {
            Console.WriteLine($"Ui folder {uiFolder} does not exist. Please supply location of project with --ui-folder [path/to/ui/]");
            return 1;
        }

        if (File.Exists(Path.Combine(uiFolder, "layout-sets.json")))
        {
            Console.WriteLine("Project already using layout sets. Skipping layout set upgrade.");
            return 0;
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
}
