using System;
using System.Linq;
using System.Threading.Tasks;
using Altinn.App.Core.Features;
using Altinn.App.Core.Interface;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

namespace Altinn.App.Api.Controllers
{
    /// <summary>
    /// Handles pdf formatting
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/data/{dataGuid:guid}/pdfformat")]
    public class PdfFormatController : ControllerBase
    {
        private readonly IInstance _instanceClient;
        private readonly IPdfFormatter _pdfFormatter;
        private readonly IAppResources _resources;
        private readonly IAppModel _appModel;
        private readonly IData _dataClient;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PagesController"/> class.
        /// </summary>
        /// <param name="instanceClient">The instance client</param>
        /// <param name="logger">A logger provided by the logging framework.</param>
        /// <param name="pdfFormatter">The pdf formatter service</param>
        /// <param name="resources">The app resource service</param>
        /// <param name="appModel">The app model service</param>
        /// <param name="dataClient">The data client</param>
        public PdfFormatController(
            IInstance instanceClient,
            IPdfFormatter pdfFormatter,
            IAppResources resources,
            IAppModel appModel,
            IData dataClient,
            ILogger<PagesController> logger)
        {
            _instanceClient = instanceClient;
            _pdfFormatter = pdfFormatter;
            _resources = resources;
            _appModel = appModel;
            _dataClient = dataClient;
            _logger = logger;
        }

        /// <summary>
        /// Get the pdf formatting
        /// </summary>
        /// <returns>The lists of pages/components to exclude from PDF</returns>
        [HttpGet()]
        public async Task<ActionResult> GetPdfFormat(
            [FromRoute] string org,
            [FromRoute] string app,
            [FromRoute] int instanceOwnerPartyId,
            [FromRoute] Guid instanceGuid,
            [FromRoute] Guid dataGuid)
        {
            Instance instance = await _instanceClient.GetInstance(app, org, instanceOwnerPartyId, instanceGuid);
            if (instance == null)
            {
                return NotFound("No current instance");
            }

            string taskId = instance.Process?.CurrentTask?.ElementId;
            if (taskId == null)
            {
                return BadRequest("No current task");
            }

            DataElement dataElement = instance.Data.FirstOrDefault(d => d.Id == dataGuid.ToString());
            if (dataElement == null)
            {
                return BadRequest("Invalid data-id");
            }

            string appModelclassRef = _resources.GetClassRefForLogicDataType(dataElement.DataType);
            Type dataType = _appModel.GetModelType(appModelclassRef);

            string layoutSetsString = _resources.GetLayoutSets();
            LayoutSets layoutSets = null;
            LayoutSet layoutSet = null;
            if (!string.IsNullOrEmpty(layoutSetsString))
            {
                layoutSets = JsonConvert.DeserializeObject<LayoutSets>(layoutSetsString)!;
                layoutSet = layoutSets.Sets?.FirstOrDefault(t => t.DataType.Equals(dataElement.DataType) && t.Tasks.Contains(taskId));
            }

            string layoutSettingsFileContent = layoutSet == null ? _resources.GetLayoutSettingsString() : _resources.GetLayoutSettingsStringForSet(layoutSet.Id);

            LayoutSettings layoutSettings = null;
            if (!string.IsNullOrEmpty(layoutSettingsFileContent))
            {
                layoutSettings = JsonConvert.DeserializeObject<LayoutSettings>(layoutSettingsFileContent);
            }

            // Ensure layoutsettings are initialized in FormatPdf
            layoutSettings ??= new();
            layoutSettings.Pages ??= new();
            layoutSettings.Pages.ExcludeFromPdf ??= new();
            layoutSettings.Pages.Order ??= new();
            layoutSettings.Components ??= new();
            layoutSettings.Components.ExcludeFromPdf ??= new();

            object data = await _dataClient.GetFormData(instanceGuid, dataType, org, app, instanceOwnerPartyId, new Guid(dataElement.Id));

            LayoutSettings formattedLayoutSettings = await _pdfFormatter.FormatPdf(layoutSettings, data);

            var result = new
            {
                ExcludedPages = formattedLayoutSettings.Pages.ExcludeFromPdf,
                ExcludedComponents = formattedLayoutSettings.Components.ExcludeFromPdf,
                PageOrder = formattedLayoutSettings.Pages.Order,
            };
            return Ok(result);
        }
    }
}