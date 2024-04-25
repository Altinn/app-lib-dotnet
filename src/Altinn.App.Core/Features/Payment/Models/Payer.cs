namespace Altinn.App.Core.Features.Payment.Models;

public class Payer
{
    public PayerPrivatePerson? PrivatePerson { get; set; }
    public PayerCompany? Company { get; set; }
    public Address? ShippingAddress { get; set; }
    public Address? BillingAddress { get; set; }
}
