namespace Altinn.App.Core.Internal.AccessManagement.Models;

// TODO: Get this from altinn urn
internal class DelegationConst
{
    internal const string Resource = "urn:altinn:resource";
    internal const string Task = "urn:altinn:task";
    internal const string ActionId = "urn:oasis:names:tc:xacml:1.0:action:action-id";
    internal const string Party = "urn:altinn:party:uuid";
}

// TODO: make complete and move or get from a registry
internal class ActionType
{
    internal const string Read = "read";
    internal const string Sign = "sign";
}
