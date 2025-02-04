namespace Altinn.App.Core.Features;

/// <summary>
/// Allows service owners to validate values of prefill from query params
/// </summary>
public interface IValidateQueryParamPrefill
{
    /// <summary>
    /// Use this method to run the validations
    /// </summary>
    /// <param name="prefill">The prefilled params to validate</param>
    /// <returns></returns>
    public Task PrefillFromQueryParamsIsValid(Dictionary<string, string>? prefill);
}
