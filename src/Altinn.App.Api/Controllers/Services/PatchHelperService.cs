#nullable enable
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Altinn.App.Api.Models;
using Altinn.App.Core.Features;
using Altinn.App.Core.Features.Validation;
using Altinn.App.Core.Helpers;
using Altinn.Platform.Storage.Interface.Models;
using Json.Patch;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.App.Api.Controllers.Services;

/// <summary>
/// Service that handles patching of form data
/// </summary>
public class PatchHelperService
{
    private readonly IValidationService _validationService;
    private readonly IEnumerable<IDataProcessor> _dataProcessors;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatchHelperService"/> class.
    /// </summary>
    public PatchHelperService(IValidationService validationService, IEnumerable<IDataProcessor> dataProcessors)
    {
        _validationService = validationService;
        _dataProcessors = dataProcessors;
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Part of <see cref="PatchFormData" /> that is separated out for testing purposes.
    /// </summary>
    /// <param name="dataType">The type of the data element</param>
    /// <param name="dataElement">The data element</param>
    /// <param name="dataPatchRequest">Container object for the <see cref="JsonPatch" /> and list of ignored validators</param>
    /// <param name="oldModel">The old state of the form data</param>
    /// <param name="instance">The instance</param>
    /// <returns>DataPatchResponse after this patch operation</returns>
    public async Task<(DataPatchResponse, ProblemDetails?)> PatchFormDataImplementation(DataType dataType, DataElement dataElement, DataPatchRequest dataPatchRequest, object oldModel, Instance instance)
    {
        var oldModelNode = JsonSerializer.SerializeToNode(oldModel);
        var patchResult = dataPatchRequest.Patch.Apply(oldModelNode);
        if (!patchResult.IsSuccess)
        {
            bool testOperationFailed = patchResult.Error!.Contains("is not equal to the indicated value.");
            return (null!, new ProblemDetails()
            {
                Title = testOperationFailed ? "Precondition in patch failed" : "Patch Operation Failed",
                Detail = patchResult.Error,
                Type = "https://datatracker.ietf.org/doc/html/rfc6902/",
                Status = testOperationFailed ? (int)HttpStatusCode.PreconditionFailed : (int)HttpStatusCode.UnprocessableContent,
                Extensions = new Dictionary<string, object?>()
                {
                    { "previousModel", oldModel },
                    { "patchOperationIndex", patchResult.Operation },
                }
            });
        }

        var (model, problem) = DeserializeModel(oldModel.GetType(), patchResult.Result!);
        if (problem is not null)
        {
            return (null!, new ProblemDetails()
            {
                Title = "Patch operation did not deserialize",
                Detail = problem,
                Type = "https://datatracker.ietf.org/doc/html/rfc6902/",
                Status = (int)HttpStatusCode.UnprocessableContent,
            });
        }

        foreach (var dataProcessor in _dataProcessors)
        {
            await dataProcessor.ProcessDataWrite(instance, Guid.Parse(dataElement.Id), model, oldModel);
        }

        // Ensure that all lists are changed from null to empty list.
        ObjectUtils.InitializeListsRecursively(model);

        var changedFields = dataPatchRequest.Patch.Operations.Select(o => o.Path.ToString()).ToList();

        var validationIssues = await _validationService.ValidateFormData(instance, dataElement, dataType, model, changedFields, dataPatchRequest.IgnoredValidators);
        var response = new DataPatchResponse
        {
            NewDataModel = model,
            ValidationIssues = validationIssues
        };
        return (response, null);
    }

    private static (object model, string? error) DeserializeModel(Type type, JsonNode patchResult)
    {
        try
        {
            var model = patchResult.Deserialize(type, JsonSerializerOptions);
            if (model is null)
            {
                return (null!, "Deserialize patched model returned null");
            }

            return (model, null);
        }
        catch (JsonException e) when (e.Message.Contains("could not be mapped to any .NET member contained in type"))
        {
            // Give better feedback when the issue is that the patch contains a path that does not exist in the model
            return (null!, e.Message);
        }
    }
}