using System.Runtime.Serialization;

namespace Altinn.App.Core.Internal.Pdf
{
    /// <summary>
    /// Class representing an exception throw when a PDF could not be created.
    /// </summary>
    [Serializable]
    public class PdfGenerationException : Exception
    {
        ///<inheritDoc/>
        public PdfGenerationException()
        {
        }

        ///<inheritDoc/>
        public PdfGenerationException(string? message) : base(message)
        {
        }

        ///<inheritDoc/>
        public PdfGenerationException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        ///<inheritDoc/>
        protected PdfGenerationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
