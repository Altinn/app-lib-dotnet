using System.Xml;
using System.Xml.Schema;
using Altinn.App.Core.Helpers;
using Altinn.App.Core.Helpers.Serialization;
using Altinn.App.Core.Internal.App;
using Altinn.App.Core.Models;
using Altinn.App.Core.Models.Validation;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Core.Features.Validation.Default;

/// <summary>
/// Validates form data against the XSD schema for the data model, if it exists
/// </summary>
public class XsdValidator : IValidator
{
    private readonly ILogger<XsdValidator> _logger;
    private readonly IAppResources _appResourceService;
    private readonly IAppMetadata _appMetadata;
    private readonly ModelSerializationService _modelSerializationService;

    /// <summary>
    /// Constructor for the expression validator
    /// </summary>
    public XsdValidator(
        ILogger<XsdValidator> logger,
        IAppResources appResourceService,
        IAppMetadata appMetadata,
        ModelSerializationService modelSerializationService
    )
    {
        _logger = logger;
        _appResourceService = appResourceService;
        _appMetadata = appMetadata;
        _modelSerializationService = modelSerializationService;
    }

    /// <summary>
    /// We implement <see cref="ShouldRunForTask"/> instead
    /// </summary>
    public string TaskId => "*";

    /// <summary>
    /// Only run for tasks that specifies a layout set
    /// </summary>
    public bool ShouldRunForTask(string taskId) =>
        _appMetadata
            .GetApplicationMetadata()
            .Result.DataTypes.Exists(dt => dt.TaskId == taskId && dt.AppLogic?.ClassRef is not null);

    /// <inheritdoc />
    public string ValidationSource => "Xsd";

    /// <inheritdoc />
    public bool NoIncrementalValidation => true;

    /// <summary>
    /// This is not used for incremental validation
    /// </summary>
    public Task<bool> HasRelevantChanges(
        IInstanceDataAccessor dataAccessor,
        string taskId,
        DataElementChanges changes
    ) => Task.FromResult(true);

    /// <inheritdoc />
    public async Task<List<ValidationIssue>> Validate(
        IInstanceDataAccessor dataAccessor,
        string taskId,
        string? language
    )
    {
        var validationIssues = new List<ValidationIssue>();
        foreach (var (dataType, dataElement) in dataAccessor.GetDataElementsForTask(taskId))
        {
            var schema = _appResourceService.GetXsdSchema(dataType.Id);
            if (schema is null)
            {
                _logger.LogInformation(
                    "No XSD schema found for data type {DataTypeId}, skipping XSD validation",
                    dataType.Id
                );
                continue;
            }
            var formData = await dataAccessor.GetFormData(dataElement);
            ObjectUtils.RemoveAltinnRowId(formData);

            var serializedFormData = _modelSerializationService.SerializeToXml(formData);
            var parsedSchema = new XmlSchemaSet();
            using (var xsdReader = XmlReader.Create(new StringReader(schema)))
            {
                parsedSchema.Add(null, xsdReader);
            }
            var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema, Schemas = parsedSchema };
            settings.ValidationEventHandler += (sender, e) =>
            {
                validationIssues.Add(
                    new ValidationIssue()
                    {
                        Code = "Xsd",
                        CustomTextKey = "backend.xsd_validation",
                        DataElementId = dataElement.Id,
                        Severity = ValidationIssueSeverity.Error,
                        CustomTextParameters = new Dictionary<string, string>()
                        {
                            { "schema", dataType.Id },
                            { "message", e.Message },
                        },
                    }
                );
            };

            using var xmlStream = new MemoryStream(serializedFormData.ToArray(), writable: false);
            using var reader = XmlReader.Create(xmlStream, settings);
            while (reader.Read()) { }
        }

        return validationIssues;
    }
}
