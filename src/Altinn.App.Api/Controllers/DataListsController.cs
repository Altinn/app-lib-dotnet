using System.Collections.Generic;
using System.Threading.Tasks;
using Altinn.App.Core.Features.DataLists;
using Altinn.App.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers
{
    /// <summary>
    /// Represents the DataLists API.
    /// </summary>
    [ApiController]
    public class DataListsController: ControllerBase
    {
        private readonly IDataListsService _dataListsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataListsController"/> class.
        /// </summary>
        /// <param name="dataListsService">Service for handling datalists</param>
        public DataListsController(IDataListsService dataListsService)
        {
            _dataListsService = dataListsService;
        }

        /// <summary>
        /// Api that exposes app related datalists
        /// </summary>
        /// <param name="id">The listId</param>
        /// <param name="language">The language selected by the user.</param>
        /// <param name="queryParams">Query parameteres supplied</param>
        /// <returns>The options list</returns>
        [HttpGet]
        [Route("/{org}/{app}/api/datalists/{id}")]
        public async Task<IActionResult> Get(
            [FromRoute] string id,
            [FromQuery] string language,
            [FromQuery] Dictionary<string, string> queryParams)
        {
            DataList dataLists = await _dataListsService.GetDataListAsync(id, language, queryParams);
            if (dataLists.ListItems == null)
            {
                return NotFound();
            }

            return Ok(dataLists);
        }
    }
}
