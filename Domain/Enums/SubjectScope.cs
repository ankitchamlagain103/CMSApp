namespace Domain.Enums
{
    // Explicit discriminator mirroring the nullable-ClassSectionId convention used by ClassSubject
    // and TeacherAssignment. Not a stored column -- computed by the mapper from ClassSectionId so
    // API consumers get a named state instead of inferring it from nullability.
    public enum SubjectScope
    {
        ClassWide = 0,
        Section = 1
    }
}
