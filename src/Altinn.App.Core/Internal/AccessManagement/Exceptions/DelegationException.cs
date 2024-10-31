using System;

namespace Altinn.App.Core.Internal.AccessManagement.Exceptions;

internal sealed class DelegationException : Exception
{
    internal DelegationException(
        string? message,
        HttpResponseMessage? response,
        string? content,
        Exception? innerException
    )
        : base(
            $"{message}: StatusCode={response?.StatusCode}\nReason={response?.ReasonPhrase}\nBody={content}\n",
            innerException
        ) { }
}
