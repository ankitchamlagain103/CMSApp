using Application.Guardians.Dtos;
using Domain.Entities;

namespace Application.Guardians
{
    public static class GuardianMapper
    {
        public static GuardianDto ToDto(Guardian guardian)
        {
            var guardianDto = new GuardianDto
            {
                Id = guardian.Id,
                FirstName = guardian.FirstName,
                LastName = guardian.LastName,
                Email = guardian.Email,
                Phone = guardian.Phone,
                Occupation = guardian.Occupation,
                Address = guardian.Address
            };

            return guardianDto;
        }
    }
}
