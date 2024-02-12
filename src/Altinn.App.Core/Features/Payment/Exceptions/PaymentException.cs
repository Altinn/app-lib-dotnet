namespace Altinn.App.Core.Features.Payment.Exceptions
{
    public class PaymentException : Exception
    {
        public PaymentException(string message) : base(message)
        {
        }
    }
}