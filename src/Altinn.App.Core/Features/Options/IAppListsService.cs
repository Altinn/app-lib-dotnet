using Altinn.App.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.App.Core.Features.Options
{
    public interface IAppListsService
    {
        Task<AppLists> GetAppListsAsync(string optionId, string language, Dictionary<string, string> keyValuePairs);
    }
}
