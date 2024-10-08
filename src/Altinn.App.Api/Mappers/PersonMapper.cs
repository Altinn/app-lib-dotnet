﻿using Altinn.App.Api.Models;
using Altinn.Platform.Register.Models;

namespace Altinn.App.Api.Mappers;

internal static class PersonMapper
{
    public static PersonSearchResponse MapToDto(Person person)
    {
        return new PersonSearchResponse
        {
            Ssn = person.SSN,
            Name = person.Name,
            FirstName = person.FirstName,
            MiddleName = person.MiddleName,
            LastName = person.LastName
        };
    }
}
