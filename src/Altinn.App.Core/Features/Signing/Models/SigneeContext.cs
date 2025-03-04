using System.Globalization;
using System.Text.Json.Serialization;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Models;
using Altinn.Platform.Storage.Interface.Models;

namespace Altinn.App.Core.Features.Signing.Models;

/// <summary>
///  Represents the context of a signee.
/// </summary>
public sealed class SigneeContext
{
    /// <summary>
    /// The task associated with the signee state.
    /// </summary>
    [JsonPropertyName("taskId")]
    public required string TaskId { get; init; }

    /// <summary>The signee.</summary>
    public required Signee Signee { get; set; }

    /// <summary>
    /// Notifications configuration.
    /// </summary>
    [JsonPropertyName("notifications")]
    public Notifications? Notifications { get; init; }

    /// <summary>
    /// The state of the signee.
    /// </summary>
    [JsonPropertyName("signeeState")]
    public required SigneeState SigneeState { get; set; }

    /// <summary>
    /// The signature document, if it exists yet.
    /// </summary>
    /// <remarks>This is not and should not be serialized and persisted in storage, it's looked up on-the-fly when the signee contexts are retrieved through <see cref="SigningService.GetSigneeContexts"/></remarks>
    [JsonIgnore]
    public SignDocument? SignDocument { get; set; }
}

/// <summary>
///  Represents the state of a signee.
/// </summary>
[JsonDerivedType(typeof(PersonSignee), typeDiscriminator: "person")]
[JsonDerivedType(typeof(OrganisationSignee), typeDiscriminator: "organisation")]
[JsonDerivedType(typeof(PersonOnBehalfOfOrgSignee), typeDiscriminator: "personOnBehalfOfOrg")]
[JsonDerivedType(typeof(SystemSignee), typeDiscriminator: "system")]
[JsonDerivedType(typeof(SelfIdentifiedUserSignee), typeDiscriminator: "selfIdentifiedUser")]
public abstract class Signee
{
    internal Party GetParty()
    {
        return this switch
        {
            PersonSignee personSignee => personSignee.Party,
            SelfIdentifiedUserSignee selfIdentifiedUserSignee => selfIdentifiedUserSignee.Party,
            OrganisationSignee organisationSignee => organisationSignee.OrgParty,
            PersonOnBehalfOfOrgSignee personOnBehalfOfOrgSignee => personOnBehalfOfOrgSignee.OnBehalfOfOrg.OrgParty,
            SystemSignee systemSignee => systemSignee.OnBehalfOfOrg.OrgParty,
            _ => throw new InvalidOperationException(
                "Signee is neither a person, an organisation, a person on behalf of an organisation, a self identified user or a system"
            ),
        };
    }

    internal static async Task<Signee> From(
        ProvidedSignee signeeParty,
        Func<PartyLookup, Task<Party>> lookupParty,
        Func<int, Task<UserProfile?>> getUserProfileUserId,
        Func<string, Task<UserProfile?>> getUserProfileSsn
    )
    {
        return signeeParty switch
        {
            Models.PersonSignee personSigneeParty => await From(
                userId: null,
                ssn: personSigneeParty.SocialSecurityNumber,
                orgNr: null,
                systemId: null,
                lookupParty,
                getUserProfileUserId,
                getUserProfileSsn
            ),
            Models.OrganisationSignee organisationSigneeParty => await From(
                userId: null,
                ssn: null,
                orgNr: organisationSigneeParty.OrganisationNumber,
                systemId: null,
                lookupParty,
                getUserProfileUserId,
                getUserProfileSsn
            ),
            _ => throw new InvalidOperationException("SigneeParty is neither a person nor an organisation"),
        };
    }

    internal static async Task<Signee> From(
        string? userId,
        string? ssn,
        string? orgNr,
        Guid? systemId,
        Func<PartyLookup, Task<Party>> lookupParty,
        Func<int, Task<UserProfile?>> getUserProfileUserId,
        Func<string, Task<UserProfile?>> getUserProfileSsn
    )
    {
        Party? orgParty = null;
        if (string.IsNullOrEmpty(orgNr) is false)
        {
            orgParty =
                await lookupParty(new PartyLookup { OrgNo = orgNr })
                ?? throw new ArgumentException($"No party found with org number {orgNr}");
        }

        OrganisationSignee? orgSignee = orgParty is not null
            ? new OrganisationSignee
            {
                OrgName = orgParty.Name,
                OrgNumber = orgParty.OrgNumber,
                OrgParty = orgParty,
            }
            : null;

        if (ssn is not null)
        {
            var userProfile =
                await getUserProfileSsn(ssn) ?? throw new ArgumentException($"No user profile found for ssn.");

            return orgSignee is not null
                ? new PersonOnBehalfOfOrgSignee
                {
                    SocialSecurityNumber = userProfile.Party.SSN,
                    FullName = userProfile.Party.Name,
                    Party = userProfile.Party,
                    OnBehalfOfOrg = orgSignee,
                    UserId = userProfile.UserId,
                    UserUuid = userProfile.UserUuid,
                }
                : new PersonSignee
                {
                    SocialSecurityNumber = userProfile.Party.SSN,
                    FullName = userProfile.Party.Name,
                    Party = userProfile.Party,
                    UserId = userProfile.UserId,
                    UserUuid = userProfile.UserUuid,
                };
        }

        if (orgSignee is not null)
        {
            return systemId.HasValue
                ? new SystemSignee { SystemId = (Guid)systemId, OnBehalfOfOrg = orgSignee }
                : orgSignee;
        }

        if (userId is not null)
        {
            UserProfile userProfile =
                await getUserProfileUserId(int.Parse(userId, CultureInfo.InvariantCulture))
                ?? throw new ArgumentException($"No user profile found for user ID {userId}");

            return new SelfIdentifiedUserSignee
            {
                UserId = userProfile.UserId,
                UserUuid = userProfile.UserUuid,
                Party = userProfile.Party,
                Username = userProfile.UserName,
                FullName = userProfile.Party.Name,
            };
        }

        throw new ArgumentException(
            "Could not find party for person or organisation. A valid SSN, UserId or OrgNr must be provided."
        );
    }

    /// <summary>
    /// A signee that is a specific person.
    /// </summary>
    public sealed class PersonSignee : Signee
    {
        public required Party Party { get; set; }
        public required int UserId { get; set; }
        public Guid? UserUuid { get; set; }

        /// <summary>
        /// The social security number.
        /// </summary>
        public required string SocialSecurityNumber { get; set; }

        /// <summary>
        /// The full name of the signee. {FirstName} {LastName} or {FirstName} {MiddleName} {LastName}.
        /// </summary>
        public required string FullName { get; set; }
    }

    /// <summary>
    /// A signee that is an organisation.
    /// </summary>
    public sealed class OrganisationSignee : Signee
    {
        /// <summary>
        /// The party of the organisation signee.
        /// </summary>
        public required Party OrgParty { get; set; }

        /// <summary>
        /// The organisation number.
        /// </summary>
        public required string OrgNumber { get; set; }

        /// <summary>
        /// The name of the organisation.
        /// </summary>
        public required string OrgName { get; set; }

        /// <summary>
        /// Converts this organisation signee to a person signee
        /// </summary>
        /// <param name="ssn"></param>
        /// <param name="getUserProfileBySsn"></param>
        /// <returns></returns>
        internal async Task<PersonOnBehalfOfOrgSignee> ToPersonOnBehalfOfOrgSignee(
            string ssn,
            Func<string, Task<UserProfile?>> getUserProfileBySsn
        )
        {
            UserProfile userProfile =
                await getUserProfileBySsn(ssn) ?? throw new ArgumentException($"No user profile found for ssn.");

            return new PersonOnBehalfOfOrgSignee
            {
                SocialSecurityNumber = ssn,
                FullName = userProfile.Party.Name,
                Party = userProfile.Party,
                UserId = userProfile.UserId,
                UserUuid = userProfile.UserUuid,
                OnBehalfOfOrg = this,
            };
        }

        internal SystemSignee ToSystemSignee(Guid systemId)
        {
            return new SystemSignee { SystemId = systemId, OnBehalfOfOrg = this };
        }
    }

    /// <summary>
    /// A person signee signing on behalf of an organisation.
    /// </summary>
    public sealed class PersonOnBehalfOfOrgSignee : Signee
    {
        public required Party Party { get; set; }
        public required int UserId { get; set; }
        public Guid? UserUuid { get; set; }

        /// <summary>
        /// The social security number.
        /// </summary>
        public required string SocialSecurityNumber { get; set; }

        /// <summary>
        /// The full name of the signee. {FirstName} {LastName} or {FirstName} {MiddleName} {LastName}.
        /// </summary>
        public required string FullName { get; set; }

        /// <summary>
        /// The organisation on behalf of which the person is signing.
        /// If this is null, the person is signing on their own behalf.
        /// </summary>
        public required OrganisationSignee OnBehalfOfOrg { get; set; }
    }

    /// <summary>
    /// A signee that is a system.
    /// </summary>
    public sealed class SystemSignee : Signee
    {
        /// <summary>
        /// The system ID of the system signee.
        /// </summary>
        public required Guid SystemId { get; set; }

        /// <summary>
        /// The organisation on behalf of which the system is signing.
        /// </summary>
        public required OrganisationSignee OnBehalfOfOrg { get; set; }
    }

    public sealed class SelfIdentifiedUserSignee : Signee
    {
        public required Party Party { get; set; }
        public required int UserId { get; set; }
        public Guid? UserUuid { get; set; }
        public string? FullName { get; set; }
        public required string Username { get; set; }
    }
}
