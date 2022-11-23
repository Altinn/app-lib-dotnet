namespace Altinn.App.Core.Internal.AppFiles;

/// <summary>
/// Custom exception for when loading of AppFiles fails
/// </summary>
[System.Serializable]
public class AppFilesLoadException : System.Exception
{
    /// <summary>
    /// Default constructor
    /// </summary>
    public AppFilesLoadException() { }
    /// <summary>
    /// Constructor with a message
    /// </summary>
    public AppFilesLoadException(string message) : base(message) { }

    // public AppFilesLoadException(string message, System.Exception inner) : base(message, inner) { }

    /// <inheritdoc />
    protected AppFilesLoadException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}