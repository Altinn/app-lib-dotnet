using Altinn.App.Api.Models;
using Altinn.Platform.Register.Models;

namespace Altinn.App.Api.Mappers;

internal static class PersonMapper
{
    public static PersonDto MapToDto(Person person)
    {
        return new PersonDto
        {
            Ssn = person.SSN,
            Name = person.Name,
            FirstName = person.FirstName,
            MiddleName = person.MiddleName,
            LastName = person.LastName
        };
    }
}
