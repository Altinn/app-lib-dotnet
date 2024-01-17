using System.Runtime.Serialization;

namespace Altinn.App.Core.Features.Payment.Providers.Nets;

[Serializable]
internal class NetsException : Exception
{
    public NetsException()
    {
    }

    public NetsException(string? message) : base(message)
    {
    }

    public NetsException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected NetsException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}