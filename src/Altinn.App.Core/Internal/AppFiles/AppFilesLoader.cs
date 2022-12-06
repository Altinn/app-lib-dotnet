using Altinn.App.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Internal.AppFiles;

/// <summary>
/// Utility to read all known app files into an <see cref="AppFilesBytes" /> without parsing and removing BOM and case sensitivty issues
/// </summary>
public class AppFilesLoader
{
    private static readonly EnumerationOptions _getFilesEnumerationOptions = new EnumerationOptions
    {
        MatchCasing = MatchCasing.CaseInsensitive,
        RecurseSubdirectories = true,
        ReturnSpecialDirectories = false,
    };

    private static readonly EnumerationOptions _getNonRecursiveEnumerationOptions = new EnumerationOptions
    {
        MatchCasing = MatchCasing.CaseInsensitive,
        ReturnSpecialDirectories = false,
    };

    private readonly DirectoryInfo _baseDirectory;

    /// <summary>
    /// Constructor
    /// </summary>
    public AppFilesLoader(IOptions<AppSettings> settings)
    {
        var path = string.IsNullOrWhiteSpace(settings.Value.AppBasePath) ? Directory.GetCurrentDirectory() : settings.Value.AppBasePath;
        _baseDirectory = new DirectoryInfo(path);
    }

    /// <summary>
    /// Load all json files from disk, and return a structure with all the byte arrays
    /// </summary>
    public async Task<AppFilesBytes> GetAppFilesBytes()
    {
        // Read applicationMetadata
        var appMetadataFileName = Path.Join("config", "applicationmetadata.json");
        var appMetadataFileInfo = _baseDirectory.GetFiles(appMetadataFileName, _getNonRecursiveEnumerationOptions);
        if (appMetadataFileInfo.Length != 1)
        {
            throw new FileNotFoundException(null, Path.Join(_baseDirectory.FullName, appMetadataFileName));
        }
        var appMetadataTask = ReadFileBytesNoBomAsync(appMetadataFileInfo.Single().FullName);

        // read policy
        var policyFileName = Path.Join("config", "authorization", "policy.xml");
        var policyFileInfo = _baseDirectory.GetFiles(policyFileName, _getNonRecursiveEnumerationOptions);
        if (policyFileInfo.Length != 1)
        {
            throw new FileNotFoundException(null, Path.Join(_baseDirectory.FullName, policyFileName));
        }
        var policyTask = ReadFileBytesNoBomAsync(policyFileInfo.Single().FullName);

        // read process
        var processFileName = Path.Join("config", "process", "process.bpmn");
        var processFileInfo = _baseDirectory.GetFiles(processFileName, _getNonRecursiveEnumerationOptions);
        if (processFileInfo.Length != 1)
        {
            throw new FileNotFoundException(null, Path.Join(_baseDirectory.FullName, processFileName));
        }
        var processTask = ReadFileBytesNoBomAsync(processFileInfo.Single().FullName);

        var textsTask = GetTexts();

        // read models
        var modelMetadata = GetModelMetadata();
        var modelPrefill = GetModelPrefill();
        var modelSchemas = GetModelSchema();
        var modelXsds = GetModelXsd();

        // read layouts
        var (layoutSets, layoutSetFiles) = await GetLayoutSets();

        return new AppFilesBytes()
        {
            ApplicationMetadata = await appMetadataTask,
            Policy = await policyTask,
            Process = await processTask,
            Texts = await textsTask,
            LayoutSetsSettings = layoutSets,
            LayoutSetFiles = layoutSetFiles,
            ModelMetadata = await modelMetadata,
            ModelPrefill = await modelPrefill,
            ModelSchemas = await modelSchemas,
            ModelXsds = await modelXsds,
        };
    }

    private async Task<Dictionary<string, byte[]>> GetDictionaryFromGlob(string pattern, Func<FileInfo, string> nameMapper)
    {
        var textsFileInfo = _baseDirectory.GetFiles(pattern, _getFilesEnumerationOptions);
        var textsPairs = await Task.WhenAll(textsFileInfo.Select(async fileInfo =>
            new KeyValuePair<string, byte[]>(nameMapper(fileInfo), await ReadFileBytesNoBomAsync(fileInfo.FullName))
        ));
        return new Dictionary<string, byte[]>(textsPairs);
    }
    private Task<Dictionary<string, byte[]>> GetTexts()
    {
        // read texts
        var textsGlobPattern = Path.Join("config", "texts", "resource.*.json");
        return GetDictionaryFromGlob(textsGlobPattern, fi => fi.Name.Split('.')[1]);
    }
    private Task<Dictionary<string, byte[]>> GetModelMetadata()
    {
        // read model
        var modelGlobPattern = Path.Join("models", "*.metadata.json");
        return GetDictionaryFromGlob(modelGlobPattern, fi => fi.Name.Split('.')[0]);
    }
    private Task<Dictionary<string, byte[]>> GetModelPrefill()
    {
        // read model
        var modelGlobPattern = Path.Join("models", "*.prefill.json");
        return GetDictionaryFromGlob(modelGlobPattern, fi => fi.Name.Split('.')[0]);
    }
    private Task<Dictionary<string, byte[]>> GetModelSchema()
    {
        // read model
        var modelGlobPattern = Path.Join("models", "*.schema.json");
        return GetDictionaryFromGlob(modelGlobPattern, fi => fi.Name.Split('.')[0]);
    }
    private Task<Dictionary<string, byte[]>> GetModelXsd()
    {
        // read model
        var modelGlobPattern = Path.Join("models", "*.Xsd.json");
        return GetDictionaryFromGlob(modelGlobPattern, fi => fi.Name.Split('.')[0]);
    }



    private async Task<(byte[]?, Dictionary<string, LayoutSetFiles>)> GetLayoutSets()
    {
        var layoutSets = new Dictionary<string, LayoutSetFiles>();
        var uiFolder = _baseDirectory.GetDirectories("ui", _getNonRecursiveEnumerationOptions).SingleOrDefault();
        if (uiFolder is null)
        {
            throw new DirectoryNotFoundException("Could not find folder \"ui\" in /app");
        }

        // read layoutSet
        var layoutSetFileInfo = uiFolder.GetFiles("layout-sets.json", _getFilesEnumerationOptions);
        var layoutSet =
                layoutSetFileInfo.Length == 1 ?
                await ReadFileBytesNoBomAsync(layoutSetFileInfo.Single().FullName) :
                null;
        if (layoutSet is null)
        {
            layoutSets.Add(string.Empty, await GetLayoutSetFiles(uiFolder));
        }
        else
        {
            foreach (var layoutSetDirectory in uiFolder.GetDirectories())
            {
                // TODO: consider using Task.WaitAll instead of awaiting in a loop
                layoutSets.Add(layoutSetDirectory.Name, await GetLayoutSetFiles(layoutSetDirectory));
            }
        }
        return (null, layoutSets);
    }
    private async Task<LayoutSetFiles> GetLayoutSetFiles(DirectoryInfo setFolder)
    {
        var settingsTask = ReadFileBytesNoBomAsync(setFolder, "Settings.json");
        var ruleHandlerTask = ReadFileOrNullBytesNoBomAsync(setFolder, "RuleHandler.js");
        var ruleConfigurationTask = ReadFileOrNullBytesNoBomAsync(setFolder, "RuleConfiguration.json");
        var pagesTask = GetLayoutPages(setFolder);
        return new LayoutSetFiles
        {
            Settings = await settingsTask,
            RuleHandler = await ruleHandlerTask,
            RuleConfiguration = await ruleConfigurationTask,
            Pages = await pagesTask
        };
    }

    private async Task<Dictionary<string, byte[]>> GetLayoutPages(DirectoryInfo setFolder)
    {
        // Read depreacted FormLayout.json without the layouts folder
        var deprecatedSingleFileLayout = setFolder.GetFiles("FormLayout.json", _getFilesEnumerationOptions);
        if (deprecatedSingleFileLayout.Length != 0)
        {
            return new Dictionary<string, byte[]>
            {
                { "FormLayout", await ReadFileBytesNoBomAsync(deprecatedSingleFileLayout.Single().FullName) }
            };
        }

        // Read files in the layouts folder.
        var layoutGlobPattern = Path.Join("layouts", "*.json");
        var layoutFileInfo = setFolder.GetFiles(layoutGlobPattern, _getFilesEnumerationOptions);
        var layoutPairs = await Task.WhenAll(layoutFileInfo.Select(async fileInfo =>
            new KeyValuePair<string, byte[]>(Path.GetFileNameWithoutExtension(fileInfo.Name), await ReadFileBytesNoBomAsync(fileInfo.FullName))
        ));
        return new Dictionary<string, byte[]>(layoutPairs);
    }


    private async Task<byte[]?> ReadFileOrNullBytesNoBomAsync(DirectoryInfo baseFolder, params string[] globParts)
    {
        var pattern = Path.Join(globParts);
        var fileInfos = baseFolder.GetFiles(pattern, _getNonRecursiveEnumerationOptions);
        if (fileInfos.Length != 1)
        {
            return null;;
        }

        return await ReadFileBytesNoBomAsync(fileInfos.Single().FullName);
    }

    private Task<byte[]> ReadFileBytesNoBomAsync(DirectoryInfo baseFolder, params string[] globParts)
    {
        var pattern = Path.Join(globParts);
        var fileInfos = baseFolder.GetFiles(pattern, _getNonRecursiveEnumerationOptions);
        if (fileInfos.Length != 1)
        {
            throw new FileNotFoundException(null, Path.Join(baseFolder.FullName, pattern));
        }

        return ReadFileBytesNoBomAsync(fileInfos.Single().FullName);
    }

    private async Task<byte[]> ReadFileBytesNoBomAsync(string filename)
    {
        var bytes = await File.ReadAllBytesAsync(filename);
        if (bytes.Length > 2 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            bytes = bytes[3..];
        }
        return bytes;
    }
}