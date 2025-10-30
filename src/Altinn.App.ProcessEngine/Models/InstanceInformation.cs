namespace Altinn.App.ProcessEngine.Models;

public sealed record InstanceInformation(string Org, string App, int InstanceOwnerPartyId, Guid InstanceGuid);
