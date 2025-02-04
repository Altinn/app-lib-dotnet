using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.DataProcessing;

/// <summary>
/// Default implementation of the IValidateQueryParamPrefill interface.
/// This implementation does not do any thing to the data
/// </summary>
public class NullQueryParamPrefillValidator : IValidateQueryParamPrefill
{
    /// <summary>
    /// Use this method to run validation on prefilled query parameter values.
    /// It's a good idea to not just let any data be prefilled into the datamode.
    /// </summary>
    /// <param name="prefill"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task PrefillFromQueryParamsIsValid(Dictionary<string, string>? prefill)
    {
        throw new NotImplementedException();
    }
}
