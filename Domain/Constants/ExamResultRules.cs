namespace Domain.Constants
{
    // Grading-policy magic numbers deliberately kept as code constants, not config -- same
    // precedent as Application/Common/Validation/DocumentFileRules (extensions/size limits).
    // The source design doc (Docs/Student_Management_System_Exam_Result_Promotion_Design.md)
    // defines the ResultStatus.Compartment outcome ("eligible for supplementary/re-examination")
    // but doesn't specify how many failed subjects still qualify -- this is that threshold.
    public static class ExamResultRules
    {
        // A student who fails at most this many subjects (out of the subjects actually included
        // in the term's calculation) is Compartment-eligible rather than an outright Fail.
        public const int CompartmentMaxFailedSubjects = 2;
    }
}
