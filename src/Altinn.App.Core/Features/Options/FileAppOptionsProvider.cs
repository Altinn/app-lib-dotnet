using System.Text.Json;
using Altinn.App.Core.Configuration;
using Altinn.App.Core.Models;
using Microsoft.Extensions.Options;

namespace Altinn.App.Core.Features.Options
{
    /// <inheritdoc/>
    public class FileAppOptionsProvider : IAppOptionsProvider
    {
        private static readonly EnumerationOptions enumerationOptions = new()
        {
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.Directory | FileAttributes.Archive | FileAttributes.Device | FileAttributes.Temporary | FileAttributes.SparseFile | FileAttributes.ReparsePoint | FileAttributes.Compressed | FileAttributes.Offline,
            IgnoreInaccessible = true,
            MatchCasing = MatchCasing.CaseInsensitive,
            RecurseSubdirectories = false,
            ReturnSpecialDirectories = false,
        };

        private readonly AppSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileAppOptionsProvider"/> class.
        /// </summary>
        public FileAppOptionsProvider(IOptions<AppSettings> settings, string optionsId)
        {
            _settings = settings.Value;
            Id = optionsId;
        }

        /// <inheritdoc/>
        public string Id { get; }

        /// <summary>
        /// Gets the options from a file in the options folder.
        /// 
        /// Throw exception if no file is found, multiple files are found or the file name is not equal to <see cref="Id"/>.
        /// Typically you should check <see cref="FileExistForOptionId"/> before calling this method to avoid exceptions.
        /// </summary>
        public async Task<AppOptions> GetAppOptionsAsync(string language, Dictionary<string, string> keyValuePairs)
        {
            if (Id.Contains('*') || Id.Contains('?'))
            {
                throw new Exception($"Invalid option id {Id}. Wildcards are not allowed.");
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Join(_settings.AppBasePath, _settings.OptionsFolder));
            var files = directoryInfo.GetFiles(Id + ".json", enumerationOptions);
            switch (files)
            {
                case []:
                    throw new Exception($"No file found with name {Id}.json in options folder.");
                case [var file] when file.Name.Equals(Id + ".json", StringComparison.InvariantCultureIgnoreCase):
                    var fileData = await File.ReadAllBytesAsync(file.FullName);
                    return new()
                    {
                        Parameters = keyValuePairs,
                        IsCacheable = true,
                        Options = JsonSerializer.Deserialize<List<AppOption>>(fileData)!,
                    };
                case [var file]:
                    throw new Exception($"{Id} is not equal to {file.Name.Replace(".json", string.Empty)}");
                default:
                    throw new Exception($"Found multiple files with name {Id}.json in options folder.");
            }
        }

        /// <summary>
        /// Checks if a file exists to provide options for the <see cref="Id"/>.
        /// </summary>
        public bool FileExistForOptionId()
        {
            if (Id.Contains('*') || Id.Contains('?'))
            {
                return false;
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Join(_settings.AppBasePath, _settings.OptionsFolder));
            var files = directoryInfo.GetFiles(Id + ".json", enumerationOptions);
            return files.Length == 1;
        }
    }
}
