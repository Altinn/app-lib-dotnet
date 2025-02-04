using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features;

public interface IValidateQueryParamPrefill
{
    /// <summary>
    /// Run events related to instantiation
    /// </summary>
    /// <remarks>
    /// For example custom prefill.
    /// </remarks>
    /// <param name="prefill">External prefill available under instansiation if supplied</param>
    public Task PrefillFromQueryParamsIsValid(Dictionary<string, string>? prefill);
}
