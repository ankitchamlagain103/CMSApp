using Application.FeeGenerationRuns.Dtos;
using Application.FeeInvoices;
using Domain.Entities;
using Domain.Enums;

namespace Application.FeeGenerationRuns
{
    public static class FeeGenerationRunMapper
    {
        // periodInvoices: every non-cancelled FeeInvoice for this run's (AcademicYear, BillingYear,
        // BillingMonth) -- headers are enough for the list row's rollup stats.
        public static FeeGenerationRunDto ToDto(FeeGenerationRun run, IReadOnlyList<FeeInvoice> periodInvoices)
        {
            var runDto = new FeeGenerationRunDto
            {
                Id = run.Id,
                AcademicYearId = run.AcademicYearId,
                AcademicYearCode = run.AcademicYear?.Code,
                BillingYear = run.BillingYear,
                BillingMonth = run.BillingMonth,
                GeneratedTs = run.GeneratedTs,
                LastRegeneratedTs = run.LastRegeneratedTs,
                Remarks = run.Remarks,
                CreatedBy = run.CreatedBy,
                CreatedTs = run.CreatedTs
            };

            var classIds = new HashSet<Guid>();
            var studentIds = new HashSet<Guid>();

            foreach (var invoice in periodInvoices)
            {
                runDto.InvoiceCount++;
                runDto.TotalNetAmount += invoice.NetAmount;
                runDto.TotalPaidAmount += invoice.PaidAmount;

                if (invoice.Status != FeeInvoiceStatus.Draft)
                {
                    runDto.TotalOutstandingAmount += invoice.NetAmount - invoice.PaidAmount;
                }

                var enrollment = invoice.Enrollment;
                if (enrollment != null)
                {
                    studentIds.Add(enrollment.StudentId);

                    if (enrollment.ClassSection != null)
                    {
                        classIds.Add(enrollment.ClassSection.AcademicClassId);
                    }
                }
            }

            runDto.ClassCount = classIds.Count;
            runDto.StudentCount = studentIds.Count;

            return runDto;
        }

        // Builds the per-class rollup list (no nested students/invoices -- that's the separate
        // class drill-down, see ToClassGroupDto) with foreach + Dictionary (house style avoids
        // GroupBy for result-list building) -- periodInvoices must include
        // Enrollment/ClassSection/AcademicClass (GetByPeriodWithDetailsAsync).
        public static FeeGenerationRunDetailDto ToDetailDto(FeeGenerationRun run, IReadOnlyList<FeeInvoice> periodInvoices)
        {
            var summaryDto = ToDto(run, periodInvoices);

            var detailDto = new FeeGenerationRunDetailDto
            {
                Id = summaryDto.Id,
                AcademicYearId = summaryDto.AcademicYearId,
                AcademicYearCode = summaryDto.AcademicYearCode,
                BillingYear = summaryDto.BillingYear,
                BillingMonth = summaryDto.BillingMonth,
                GeneratedTs = summaryDto.GeneratedTs,
                LastRegeneratedTs = summaryDto.LastRegeneratedTs,
                Remarks = summaryDto.Remarks,
                CreatedBy = summaryDto.CreatedBy,
                CreatedTs = summaryDto.CreatedTs,
                UpdatedBy = run.UpdatedBy,
                UpdatedTs = run.UpdatedTs,
                InvoiceCount = summaryDto.InvoiceCount,
                ClassCount = summaryDto.ClassCount,
                StudentCount = summaryDto.StudentCount,
                TotalNetAmount = summaryDto.TotalNetAmount,
                TotalPaidAmount = summaryDto.TotalPaidAmount,
                TotalOutstandingAmount = summaryDto.TotalOutstandingAmount
            };

            var classSummariesById = new Dictionary<Guid, FeeGenerationClassSummaryDto>();
            var studentIdsByClass = new Dictionary<Guid, HashSet<Guid>>();

            foreach (var invoice in periodInvoices)
            {
                var enrollment = invoice.Enrollment;
                if (enrollment == null || enrollment.ClassSection == null)
                {
                    continue;
                }

                var academicClassId = enrollment.ClassSection.AcademicClassId;

                if (!classSummariesById.TryGetValue(academicClassId, out var classSummary))
                {
                    classSummary = new FeeGenerationClassSummaryDto
                    {
                        AcademicClassId = academicClassId,
                        GradeCode = enrollment.ClassSection.AcademicClass?.GradeCode
                    };
                    classSummariesById[academicClassId] = classSummary;
                    studentIdsByClass[academicClassId] = new HashSet<Guid>();
                }

                studentIdsByClass[academicClassId].Add(enrollment.Id);

                classSummary.InvoiceCount++;
                classSummary.TotalNetAmount += invoice.NetAmount;
                classSummary.TotalPaidAmount += invoice.PaidAmount;

                if (invoice.Status == FeeInvoiceStatus.Draft)
                {
                    classSummary.DraftInvoiceCount++;
                }
                else
                {
                    classSummary.TotalOutstandingAmount += invoice.NetAmount - invoice.PaidAmount;
                }
            }

            foreach (var classAndStudentIds in studentIdsByClass)
            {
                classSummariesById[classAndStudentIds.Key].StudentCount = classAndStudentIds.Value.Count;
            }

            foreach (var classSummary in classSummariesById.Values)
            {
                detailDto.Classes.Add(classSummary);
            }

            return detailDto;
        }

        // Builds one class's student -> invoice tree -- the drill-down behind
        // GET /api/feegenerationruns/{id}/classes/{academicClassId}. classInvoices must include
        // Enrollment/Student/ClassSection/AcademicClass (GetByPeriodForClassWithDetailsAsync) and
        // must already be scoped to the one class.
        public static FeeGenerationClassGroupDto ToClassGroupDto(Guid academicClassId, string gradeCode, IReadOnlyList<FeeInvoice> classInvoices)
        {
            var classGroup = new FeeGenerationClassGroupDto
            {
                AcademicClassId = academicClassId,
                GradeCode = gradeCode
            };

            var studentGroupsByEnrollmentId = new Dictionary<Guid, FeeGenerationStudentGroupDto>();

            foreach (var invoice in classInvoices)
            {
                var enrollment = invoice.Enrollment;
                if (enrollment == null)
                {
                    continue;
                }

                if (!studentGroupsByEnrollmentId.TryGetValue(enrollment.Id, out var studentGroup))
                {
                    studentGroup = new FeeGenerationStudentGroupDto
                    {
                        EnrollmentId = enrollment.Id,
                        StudentId = enrollment.StudentId,
                        StudentName = BuildStudentName(enrollment.Student),
                        AdmissionNo = enrollment.Student?.AdmissionNo,
                        SectionCode = enrollment.ClassSection?.SectionCode
                    };
                    studentGroupsByEnrollmentId[enrollment.Id] = studentGroup;
                    classGroup.Students.Add(studentGroup);
                }

                var invoiceDto = FeeInvoiceMapper.ToDto(invoice, includeLines: false);
                studentGroup.Invoices.Add(invoiceDto);

                studentGroup.TotalNetAmount += invoice.NetAmount;
                studentGroup.TotalPaidAmount += invoice.PaidAmount;

                classGroup.InvoiceCount++;
                classGroup.TotalNetAmount += invoice.NetAmount;
                classGroup.TotalPaidAmount += invoice.PaidAmount;

                if (invoice.Status == FeeInvoiceStatus.Draft)
                {
                    classGroup.DraftInvoiceCount++;
                }
                else
                {
                    var outstanding = invoice.NetAmount - invoice.PaidAmount;
                    studentGroup.TotalOutstandingAmount += outstanding;
                    classGroup.TotalOutstandingAmount += outstanding;
                }
            }

            classGroup.StudentCount = studentGroupsByEnrollmentId.Count;

            return classGroup;
        }

        private static string BuildStudentName(Student student)
        {
            if (student == null)
            {
                return null;
            }

            var nameParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(student.FirstName))
            {
                nameParts.Add(student.FirstName);
            }

            if (!string.IsNullOrWhiteSpace(student.MiddleName))
            {
                nameParts.Add(student.MiddleName);
            }

            if (!string.IsNullOrWhiteSpace(student.LastName))
            {
                nameParts.Add(student.LastName);
            }

            var fullName = string.Join(" ", nameParts);
            return fullName;
        }
    }
}
