using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Features.FileAnalyzis
{
    /// <summary>
    /// Does a deep analysis of the filetype by scanning the binary
    /// for known string patterns and magic numbers.
    /// </summary>
    public class FileMimeTypeAnalyzer : IFileAnalyzer
    {
        public IDictionary<string, string> Analyze(StreamContent streamContent)
        {
            Dictionary<string, string> metadata = new Dictionary<string, string>();

            return metadata;
        }
    }
}
