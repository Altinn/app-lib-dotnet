using System.Collections.Generic;
using System.Threading.Tasks;
using Altinn.App.Core.Features.Options;
using Altinn.App.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers
{
    /// <summary>
    /// Represents the AppLists API.
    /// </summary>
    [ApiController]
    public class AppListsController: ControllerBase
    {
        private readonly IAppListsService _appListsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppListsController"/> class.
        /// </summary>
        /// <param name="appListsService">Service for handling app options</param>
        public AppListsController(IAppListsService appListsService)
        {
            _appListsService = appListsService;
        }

        /// <summary>
        /// Api that exposes app related options
        /// </summary>
        /// <param name="listId">The listId</param>
        /// <param name="language">The language selected by the user.</param>
        /// <param name="queryParams">Query parameteres supplied</param>
        /// <returns>The options list</returns>
        [HttpGet]
        [Route("/{org}/{app}/api/lists/{listId}")]
        public async Task<IActionResult> Get(
            [FromRoute] string listId,
            [FromQuery] string language,
            [FromQuery] Dictionary<string, string> queryParams)
        {
            AppLists appLists = await _appListsService.GetAppListsAsync(listId, language, queryParams);
            if (appLists.ListItems == null)
            {
                return NotFound();
            }

            return Ok(appLists);
        }
    }
}
