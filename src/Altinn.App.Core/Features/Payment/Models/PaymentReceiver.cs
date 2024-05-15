namespace Altinn.App.Core.Features.Payment.Models;

public class PaymentReceiver
{
    public string? OrganisationNumber { get; set; }
    public string? Name { get; set; }
    public Address? PostalAddress { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? Email { get; set; }
    public PhoneNumber? PhoneNumber { get; set; }
}
