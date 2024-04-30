namespace Altinn.App.Core.Features.Payment.Models;

public class PayerCompany
{
    public string? OrganisationNumber { get; set; }
    public string? Name { get; set; }
    public PayerPrivatePerson? ContactPerson { get; set; }
}
