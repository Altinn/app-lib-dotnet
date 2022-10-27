using System.Collections.Generic;
using System.Threading.Tasks;
using Altinn.App.Core.Features.Options;
using Altinn.App.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers
{
    /// <summary>
    /// Represents the TableOptions API.
    /// </summary>
    [ApiController]
    public class TableOptionsController: ControllerBase
    {
        private readonly IAppOptionsService _appOptionsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableOptionsController"/> class.
        /// </summary>
        /// <param name="appOptionsService">Service for handling app options</param>
        public TableOptionsController(IAppOptionsService appOptionsService)
        {
            _appOptionsService = appOptionsService;
        }

        /// <summary>
        /// Api that exposes app related options
        /// </summary>
        /// <param name="optionsId">The optionsId</param>
        /// <param name="language">The language selected by the user.</param>
        /// <param name="queryParams">Query parameteres supplied</param>
        /// <returns>The options list</returns>
        [HttpGet]
        [Route("/{org}/{app}/api/options/table/{optionsId}")]
        public async Task<IActionResult> GetTableOptions(
            [FromRoute] string optionsId,
            [FromQuery] string language,
            [FromQuery] Dictionary<string, string> queryParams)
        {
            AppTableOptions appOptions = await _appOptionsService.GetTableOptionsAsync(optionsId, language, queryParams);
            if (appOptions.ListItems == null)
            {
                return NotFound();
            }

            return Ok(appOptions);
        }
    }
}
