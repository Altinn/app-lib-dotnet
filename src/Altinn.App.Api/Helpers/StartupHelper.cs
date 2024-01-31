using System.Reflection;

namespace Altinn.App.Api.Helpers;

/// <summary>
/// Static helper methods used in Program.cs during app startup
/// </summary>
public static class StartupHelper
{
    /// <summary>
    /// Delegate for swagger funciton
    /// </summary>
    public delegate void SwaggerIncludeXmlComments(string filepath, bool a);
    
    /// <summary>
    /// Includes comments in swagger based on XML comment files 
    /// </summary>
    /// <param name="swaggerDelegate">Delegate for passing SwaggerGenOptions.IncludeXmlComments function</param>
    public static void IncludeXmlComments(SwaggerIncludeXmlComments swaggerDelegate)
    {
        try
        {
            string fileName = $"{Assembly.GetCallingAssembly().GetName().Name}.xml";
            string fullFilePath = Path.Combine(AppContext.BaseDirectory, fileName);
            swaggerDelegate(fullFilePath, false);
            string fullFilePathApi = Path.Combine(AppContext.BaseDirectory, "Altinn.App.Api.xml");
            swaggerDelegate(fullFilePathApi, false);
        }
        catch (Exception)
        {
            // Swagger documentation not generated
        }
    }
}