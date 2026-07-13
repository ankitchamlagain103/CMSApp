using Application.Students.Dtos;
using Domain.Entities;

namespace Application.Students
{
    public static class StudentMapper
    {
        public static StudentDto ToDto(Student student)
        {
            var studentDto = new StudentDto
            {
                Id = student.Id,
                AdmissionNo = student.AdmissionNo,
                FirstName = student.FirstName,
                MiddleName = student.MiddleName,
                LastName = student.LastName,
                Gender = student.Gender,
                DateOfBirth = student.DateOfBirth,
                Email = student.Email,
                Phone = student.Phone,
                Address = student.Address,
                AdmissionDate = student.AdmissionDate,
                Status = student.Status
            };

            return studentDto;
        }

        // Detail-shape overload: same as above plus the guardian links (each with its Guardian
        // navigation loaded) flattened into the DTO.
        public static StudentDto ToDto(Student student, IReadOnlyList<StudentGuardian> guardianLinks)
        {
            var studentDto = ToDto(student);

            var guardianDtos = new List<StudentGuardianDto>();
            foreach (var guardianLink in guardianLinks)
            {
                var guardianDto = ToGuardianLinkDto(guardianLink);
                guardianDtos.Add(guardianDto);
            }

            studentDto.Guardians = guardianDtos;
            return studentDto;
        }

        // Expects the link's Guardian navigation to be loaded (the repository includes it).
        public static StudentGuardianDto ToGuardianLinkDto(StudentGuardian link)
        {
            var studentGuardianDto = new StudentGuardianDto
            {
                Id = link.Id,
                StudentId = link.StudentId,
                GuardianId = link.GuardianId,
                RelationshipCode = link.RelationshipCode,
                IsPrimary = link.IsPrimary,
                GuardianFirstName = link.Guardian != null ? link.Guardian.FirstName : null,
                GuardianLastName = link.Guardian != null ? link.Guardian.LastName : null,
                GuardianPhone = link.Guardian != null ? link.Guardian.Phone : null,
                GuardianEmail = link.Guardian != null ? link.Guardian.Email : null
            };

            return studentGuardianDto;
        }
    }
}
