namespace Altinn.App.Core.Models.UserAction;

/// <summary>
/// Defines an action that should be performed by frontend
/// </summary>
public class FrontendAction
{
    /// <summary>
    /// Name of the action
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Metadata for the action
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
    
    /// <summary>
    /// Creates a gotoPage action
    /// </summary>
    /// <param name="frontendPage"></param>
    /// <returns></returns>
    public static FrontendAction GotoPage(string frontendPage)
    {
        var frontendAction = new FrontendAction()
        {
            Name = "gotoPage",
            Metadata = new Dictionary<string, object>()
        };
        frontendAction.Metadata.Add("page", frontendPage);
        return frontendAction;
    }
}