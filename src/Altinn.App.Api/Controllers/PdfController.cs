using System.Text.Json;
using Altinn.App.Api.Infrastructure.RateLimiting;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Internal.AppModel;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Core.Internal.Instances;
using Altinn.App.Core.Internal.Pdf;
using Altinn.App.Core.Internal.Process;
using Altinn.App.Core.Internal.Process.Elements;
using Altinn.App.Core.Internal.Process.Elements.AltinnExtensionProperties;
using Altinn.App.Core.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Altinn.App.Api.Controllers;

/// <summary>
/// Handles PDF related operations
/// </summary>
[Authorize]
[ApiController]
public class PdfController : ControllerBase
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IInstanceClient _instanceClient;
#pragma warning disable CS0618 // Type or member is obsolete
    private readonly IPdfFormatter _pdfFormatter;
    private readonly IAppResources _resources;
    private readonly IDataClient _dataClient;
    private readonly IPdfService _pdfService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfController"/> class.
    /// </summary>
    /// <param name="instanceClient">The instance client</param>
    /// <param name="pdfFormatter">The pdf formatter service</param>
    /// <param name="resources">The app resource service</param>
    /// <param name="appModel">Unused, kept so the constructor signature does not change</param>
    /// <param name="dataClient">The data client</param>
    /// <param name="pdfService">The PDF service</param>
    public PdfController(
        IInstanceClient instanceClient,
#pragma warning disable CS0618 // Type or member is obsolete
        IPdfFormatter pdfFormatter,
        IAppResources resources,
        IAppModel appModel,
        IDataClient dataClient,
        IPdfService pdfService
    )
    {
        _instanceClient = instanceClient;
        _pdfFormatter = pdfFormatter;
        _resources = resources;
        _dataClient = dataClient;
        _pdfService = pdfService;
    }

    /// <summary>
    /// Generate a preview of the PDF for the current task.
    /// Retained for binary compatibility with v8 callers.
    /// </summary>
    [NonAction]
    public Task<ActionResult> GetPdfPreview(string org, string app, int instanceOwnerPartyId, Guid instanceGuid) =>
        GetPdfPreview(org, app, instanceOwnerPartyId, instanceGuid, null, null, null, CancellationToken.None);

    /// <summary>
    /// Generate a preview of the current task, or a PDF or subform PDF service task.
    /// </summary>
    /// <param name="org">The application organization.</param>
    /// <param name="app">The application name.</param>
    /// <param name="instanceOwnerPartyId">The instance owner's party id.</param>
    /// <param name="instanceGuid">The instance id.</param>
    /// <param name="taskId">The PDF or subform PDF service task to preview. Defaults to the current task.</param>
    /// <param name="dataElementId">The subform data element. Required for a subform PDF service task.</param>
    /// <param name="language">The preview language. Defaults to the user's language.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest, "text/plain")]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound, "text/plain")]
    [ProducesResponseType(typeof(void), StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PdfPreviewRateLimiterPolicy.Name)]
    [HttpGet("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/pdf/preview")]
    public async Task<ActionResult> GetPdfPreview(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromQuery] string? taskId,
        [FromQuery] Guid? dataElementId,
        [FromQuery] string? language,
        CancellationToken cancellationToken = default
    )
    {
        Instance? instance = await _instanceClient.GetInstance(
            app,
            org,
            instanceOwnerPartyId,
            instanceGuid,
            authenticationMethod: null,
            cancellationToken
        );
        string? currentTaskId = instance?.Process?.CurrentTask?.ElementId;
        if (instance is null || currentTaskId is null)
        {
            return NotFound("Did not find instance or task");
        }

        if (taskId is null)
        {
            // Preserve the existing v8 service, including any app-provided implementation.
            // It already reads language/lang from the request.
            Stream pdfContent = await _pdfService.GeneratePdf(instance, currentTaskId, true, cancellationToken);
            return new FileStreamResult(pdfContent, "application/pdf");
        }

        // Resolve the new internal service without changing the existing public constructor.
        var processReader = HttpContext.RequestServices.GetRequiredService<IProcessReader>();
        if (processReader.GetFlowElement(taskId) is not ProcessTask task)
        {
            return NotFound("Did not find task");
        }

        AltinnTaskExtension? taskExtension = task.ExtensionElements?.TaskExtension;
        SubformPdfContext? subformContext = null;
        List<string>? autoPdfTaskIds = null;
        if (taskExtension?.TaskType == "pdf")
        {
            autoPdfTaskIds = taskExtension.PdfConfiguration?.AutoPdfTaskIds;
        }
        else if (taskExtension?.TaskType == "subformPdf")
        {
            ValidAltinnSubformPdfConfiguration subformConfig = (
                taskExtension.SubformPdfConfiguration
                ?? throw new ApplicationConfigException(
                    "The subformPdfConfig node is missing in the subform pdf process task configuration."
                )
            ).Validate();
            string? subformId = dataElementId?.ToString();
            DataElement? subform = instance.Data.Find(element => element.Id == subformId);
            if (subform is null || subform.DataType != subformConfig.SubformDataTypeId)
            {
                return BadRequest(
                    $"dataElementId must be the id of a data element of type {subformConfig.SubformDataTypeId}"
                );
            }

            subformContext = new SubformPdfContext(subformConfig.SubformComponentId, subform.Id);
        }
        else
        {
            return BadRequest("taskId must identify a PDF or subform PDF service task");
        }

        var previewService = HttpContext.RequestServices.GetRequiredService<IPdfPreviewService>();
        Stream preview = await previewService.GeneratePreview(
            instance,
            taskId,
            language,
            subformContext,
            autoPdfTaskIds,
            cancellationToken
        );
        return new FileStreamResult(preview, "application/pdf");
    }

    /// <summary>
    /// Get the pdf formatting
    /// </summary>
    /// <returns>The lists of pages/components to exclude from PDF</returns>
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound, "text/plain")]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict, "text/plain")]
    [HttpGet("{org}/{app}/instances/{instanceOwnerPartyId:int}/{instanceGuid:guid}/data/{dataGuid}/pdf/format")]
    public async Task<ActionResult> GetPdfFormat(
        [FromRoute] string org,
        [FromRoute] string app,
        [FromRoute] int instanceOwnerPartyId,
        [FromRoute] Guid instanceGuid,
        [FromRoute] Guid dataGuid
    )
    {
        Instance instance = await _instanceClient.GetInstance(
            app,
            org,
            instanceOwnerPartyId,
            instanceGuid,
            authenticationMethod: null,
            CancellationToken.None
        );
        if (instance == null)
        {
            return NotFound("Did not find instance");
        }

        string? taskId = instance.Process?.CurrentTask?.ElementId;
        if (taskId == null)
        {
            return Conflict("Instance does not have a valid currentTask");
        }

        DataElement? dataElement = instance.Data.FirstOrDefault(d => d.Id == dataGuid.ToString());
        if (dataElement == null)
        {
            return NotFound("Did not find data element");
        }

        string layoutSetsString = _resources.GetLayoutSets();
        LayoutSets? layoutSets = null;
        LayoutSet? layoutSet = null;
        if (!string.IsNullOrEmpty(layoutSetsString))
        {
            layoutSets =
                JsonSerializer.Deserialize<LayoutSets>(layoutSetsString, _jsonSerializerOptions)
                ?? throw new JsonException("Could not deserialize LayoutSets");
            layoutSet = layoutSets.Sets?.FirstOrDefault(t =>
                t.DataType.Equals(dataElement.DataType, StringComparison.Ordinal) && t.Tasks?.Contains(taskId) is true
            );
        }

        string? layoutSettingsFileContent =
            layoutSet == null
                ? _resources.GetLayoutSettingsString()
                : _resources.GetLayoutSettingsStringForSet(layoutSet.Id);

        LayoutSettings? layoutSettings = null;
        if (!string.IsNullOrEmpty(layoutSettingsFileContent))
        {
            layoutSettings = JsonSerializer.Deserialize<LayoutSettings>(
                layoutSettingsFileContent,
                _jsonSerializerOptions
            );
        }

        // Ensure layoutsettings are initialized in FormatPdf
        layoutSettings ??= new();
        layoutSettings.Pages ??= new();
        layoutSettings.Pages.ExcludeFromPdf ??= new();
        layoutSettings.Components ??= new();
        layoutSettings.Components.ExcludeFromPdf ??= new();

        object data = await _dataClient.GetFormData(
            instance,
            dataElement,
            authenticationMethod: null,
            CancellationToken.None
        );

        layoutSettings = await _pdfFormatter.FormatPdf(layoutSettings, data, instance, layoutSet);

        var result = new
        {
            ExcludedPages = layoutSettings?.Pages?.ExcludeFromPdf ?? new List<string>(),
            ExcludedComponents = layoutSettings?.Components?.ExcludeFromPdf ?? new List<string>(),
        };
        return Ok(result);
    }
}
