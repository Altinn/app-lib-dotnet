using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Altinn.Platform.Register.Models;

namespace Altinn.App.Api.Helpers;

/// <summary>
/// Produces copies of <see cref="Party"/> objects with their social security numbers (SSNs) masked,
/// so the full SSN is never leaked in HTTP responses (e.g. "12345678901" becomes "123456*****").
/// The original objects (which may be cached and used server-side) are never modified.
/// </summary>
internal static class PartySsnMasking
{
    /// <summary>
    /// Number of leading characters kept visible; everything after is replaced with '*'.
    /// </summary>
    private const int VisibleDigits = 6;

    // The properties we copy when cloning. Cached once per type so we don't reflect on every call.
    private static readonly PropertyInfo[] _partyProperties = CopyableProperties(typeof(Party));
    private static readonly PropertyInfo[] _personProperties = CopyableProperties(typeof(Person));

    /// <summary>
    /// Returns a new list where every party is a masked copy of the corresponding input party.
    /// </summary>
    public static List<Party> MaskParties(IEnumerable<Party> parties)
    {
        List<Party> maskedParties = new List<Party>();
        foreach (Party party in parties)
        {
            maskedParties.Add(MaskParty(party));
        }

        return maskedParties;
    }

    /// <summary>
    /// Returns a copy of <paramref name="party"/> with the SSN masked, including the nested
    /// <see cref="Party.Person"/> and any <see cref="Party.ChildParties"/>. Returns <c>null</c> if
    /// <paramref name="party"/> is <c>null</c>.
    /// </summary>
    [return: NotNullIfNotNull(nameof(party))]
    public static Party? MaskParty(Party? party)
    {
        if (party is null)
        {
            return null;
        }

        // Copy every field as-is, then transform only the parts that carry an SSN.
        Party clone = new Party();
        CopyProperties(_partyProperties, party, clone);

        clone.SSN = Mask(party.SSN);
        clone.Person = MaskPerson(party.Person);
        clone.ChildParties = MaskChildParties(party.ChildParties);

        return clone;
    }

    /// <summary>
    /// Masks an SSN by keeping the first <see cref="VisibleDigits"/> characters visible and replacing
    /// the rest with '*', e.g. "12345678901" becomes "123456*****". Values of <see cref="VisibleDigits"/>
    /// characters or fewer are fully masked, and <c>null</c>/empty values are returned as-is.
    /// </summary>
    public static string? Mask(string? ssn)
    {
        if (string.IsNullOrEmpty(ssn))
        {
            return ssn;
        }

        if (ssn.Length <= VisibleDigits)
        {
            // Too short to keep any part visible, so mask the whole thing.
            return new string('*', ssn.Length);
        }

        string visiblePart = ssn.Substring(0, VisibleDigits);
        string maskedPart = new string('*', ssn.Length - VisibleDigits);
        return visiblePart + maskedPart;
    }

    private static List<Party>? MaskChildParties(List<Party>? childParties)
    {
        if (childParties is null)
        {
            return null;
        }

        return MaskParties(childParties);
    }

    /// <summary>
    /// Returns a copy of <paramref name="person"/> with only the SSN masked; all other fields are
    /// copied as-is. Returns <c>null</c> if <paramref name="person"/> is <c>null</c>.
    /// </summary>
    private static Person? MaskPerson(Person? person)
    {
        if (person is null)
        {
            return null;
        }

        Person clone = new Person();
        CopyProperties(_personProperties, person, clone);

        clone.SSN = Mask(person.SSN);

        return clone;
    }

    /// <summary>
    /// Returns the readable and writable, non-indexer properties of <paramref name="type"/>;
    /// these are the ones we can copy when cloning.
    /// </summary>
    private static PropertyInfo[] CopyableProperties(Type type)
    {
        List<PropertyInfo> copyable = new List<PropertyInfo>();
        foreach (PropertyInfo property in type.GetProperties())
        {
            if (property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
            {
                copyable.Add(property);
            }
        }

        return copyable.ToArray();
    }

    private static void CopyProperties(PropertyInfo[] properties, object source, object destination)
    {
        foreach (PropertyInfo property in properties)
        {
            property.SetValue(destination, property.GetValue(source));
        }
    }
}
