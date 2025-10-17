namespace Altinn.App.Clients.Fiks.FiksArkiv.Models;

public sealed record FiksArkivRecipient(Guid AccountId, string? Identifier, string? OrgNumber, string? Name);
