using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class EnrollmentRepository : Repository<Enrollment, Guid>, IEnrollmentRepository
    {
        public EnrollmentRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<Enrollment>> GetPagedByFilterAsync(EnrollmentFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            // ClassSection + its AcademicClass are included so the DTO can flatten grade/section
            // info without a second call per row.
            IQueryable<Enrollment> enrollmentsQuery = DbSet
                .Include(enrollment => enrollment.Student)
                .Include(enrollment => enrollment.ClassSection)
                    .ThenInclude(section => section.AcademicClass);

            if (filter.StudentId.HasValue)
            {
                enrollmentsQuery = enrollmentsQuery.Where(enrollment => enrollment.StudentId == filter.StudentId.Value);
            }

            if (filter.AcademicClassId.HasValue)
            {
                enrollmentsQuery = enrollmentsQuery.Where(enrollment => enrollment.ClassSection.AcademicClassId == filter.AcademicClassId.Value);
            }

            if (filter.ClassSectionId.HasValue)
            {
                enrollmentsQuery = enrollmentsQuery.Where(enrollment => enrollment.ClassSectionId == filter.ClassSectionId.Value);
            }

            if (filter.AcademicYearId.HasValue)
            {
                enrollmentsQuery = enrollmentsQuery.Where(enrollment => enrollment.ClassSection.AcademicClass.AcademicYearId == filter.AcademicYearId.Value);
            }

            if (filter.Status.HasValue)
            {
                enrollmentsQuery = enrollmentsQuery.Where(enrollment => enrollment.Status == filter.Status.Value);
            }

            if (filter.FromDate.HasValue || filter.ToDate.HasValue)
            {
                enrollmentsQuery = ApplyDateRange(enrollmentsQuery, filter);
            }

            var totalCount = await enrollmentsQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await enrollmentsQuery
                .OrderBy(enrollment => enrollment.RollNumber)
                .ThenBy(enrollment => enrollment.Id)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<Enrollment>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        // EnrollmentDate compares against the enrollment's own date; CreatedDate compares against
        // the audit column (ToDate is inclusive of the whole day).
        private static IQueryable<Enrollment> ApplyDateRange(IQueryable<Enrollment> query, EnrollmentFilter filter)
        {
            if (filter.DateField == EnrollmentDateField.CreatedDate)
            {
                if (filter.FromDate.HasValue)
                {
                    var fromTs = new DateTimeOffset(filter.FromDate.Value.Date, TimeSpan.Zero);
                    query = query.Where(enrollment => enrollment.CreatedTs >= fromTs);
                }

                if (filter.ToDate.HasValue)
                {
                    var toTs = new DateTimeOffset(filter.ToDate.Value.Date.AddDays(1), TimeSpan.Zero);
                    query = query.Where(enrollment => enrollment.CreatedTs < toTs);
                }

                return query;
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(enrollment => enrollment.EnrollmentDate.HasValue && enrollment.EnrollmentDate.Value >= filter.FromDate.Value.Date);
            }

            if (filter.ToDate.HasValue)
            {
                query = query.Where(enrollment => enrollment.EnrollmentDate.HasValue && enrollment.EnrollmentDate.Value < filter.ToDate.Value.Date.AddDays(1));
            }

            return query;
        }

        public async Task<Enrollment> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var enrollment = await DbSet
                .Include(e => e.Student)
                .Include(e => e.ClassSection)
                    .ThenInclude(section => section.AcademicClass)
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            return enrollment;
        }

        public async Task<IReadOnlyList<Enrollment>> GetActiveByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            // The student profile's "current class" lookup: active rows with the year included
            // so the caller can prefer the IsCurrent academic year.
            var activeEnrollments = await DbSet
                .Include(e => e.ClassSection)
                    .ThenInclude(section => section.AcademicClass)
                        .ThenInclude(academicClass => academicClass.AcademicYear)
                .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Enrolled)
                .ToListAsync(cancellationToken);

            return activeEnrollments;
        }

        public async Task<IReadOnlyList<Enrollment>> GetHistoryByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            // Full schooling history: every enrollment regardless of status, oldest year first.
            var history = await DbSet
                .Include(e => e.ClassSection)
                    .ThenInclude(section => section.AcademicClass)
                        .ThenInclude(academicClass => academicClass.AcademicYear)
                .Where(e => e.StudentId == studentId)
                .OrderBy(e => e.ClassSection.AcademicClass.AcademicYear.StartDate)
                .ThenBy(e => e.EnrollmentDate)
                .ToListAsync(cancellationToken);

            return history;
        }

        public async Task<bool> EnrollmentExistsAsync(Guid studentId, Guid classSectionId, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the composite unique index still sees soft-deleted rows.
            var enrollmentExists = await DbSet
                .IgnoreQueryFilters()
                .AnyAsync(enrollment => enrollment.StudentId == studentId
                    && enrollment.ClassSectionId == classSectionId, cancellationToken);

            return enrollmentExists;
        }

        public async Task<bool> HasActiveEnrollmentInYearAsync(Guid studentId, Guid academicYearId, Guid? excludeEnrollmentId, CancellationToken cancellationToken = default)
        {
            // One Enrolled-status row per student per academic year -- the guard against the same
            // student sitting in two grades/sections of one year at the same time.
            IQueryable<Enrollment> activeQuery = DbSet
                .Where(enrollment => enrollment.StudentId == studentId
                    && enrollment.Status == EnrollmentStatus.Enrolled
                    && enrollment.ClassSection.AcademicClass.AcademicYearId == academicYearId);

            if (excludeEnrollmentId.HasValue)
            {
                activeQuery = activeQuery.Where(enrollment => enrollment.Id != excludeEnrollmentId.Value);
            }

            var hasActiveEnrollment = await activeQuery.AnyAsync(cancellationToken);
            return hasActiveEnrollment;
        }

        public async Task<int> CountActiveBySectionAsync(Guid classSectionId, CancellationToken cancellationToken = default)
        {
            var activeCount = await DbSet
                .CountAsync(enrollment => enrollment.ClassSectionId == classSectionId
                    && enrollment.Status == EnrollmentStatus.Enrolled, cancellationToken);

            return activeCount;
        }

        public async Task<bool> RollNumberExistsInSectionAsync(Guid classSectionId, string rollNumber, Guid? excludeEnrollmentId, CancellationToken cancellationToken = default)
        {
            IQueryable<Enrollment> rollNumberQuery = DbSet
                .Where(enrollment => enrollment.ClassSectionId == classSectionId
                    && enrollment.RollNumber == rollNumber);

            if (excludeEnrollmentId.HasValue)
            {
                rollNumberQuery = rollNumberQuery.Where(enrollment => enrollment.Id != excludeEnrollmentId.Value);
            }

            var rollNumberExists = await rollNumberQuery.AnyAsync(cancellationToken);
            return rollNumberExists;
        }

        public async Task<IReadOnlyList<EnrollmentSubject>> GetElectiveSubjectsAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var electiveSubjects = await DbContext.Set<EnrollmentSubject>()
                .Include(electiveSubject => electiveSubject.ClassSubject)
                .Where(electiveSubject => electiveSubject.EnrollmentId == enrollmentId)
                .ToListAsync(cancellationToken);

            return electiveSubjects;
        }

        public async Task<EnrollmentSubject> GetElectiveSubjectByIdAsync(Guid electiveSubjectId, CancellationToken cancellationToken = default)
        {
            var electiveSubject = await DbContext.Set<EnrollmentSubject>()
                .FirstOrDefaultAsync(es => es.Id == electiveSubjectId, cancellationToken);

            return electiveSubject;
        }

        public async Task<bool> ElectiveSubjectExistsAsync(Guid enrollmentId, Guid classSubjectId, CancellationToken cancellationToken = default)
        {
            var electiveSubjectExists = await DbContext.Set<EnrollmentSubject>()
                .AnyAsync(es => es.EnrollmentId == enrollmentId && es.ClassSubjectId == classSubjectId, cancellationToken);

            return electiveSubjectExists;
        }

        public async Task AddElectiveSubjectAsync(EnrollmentSubject electiveSubject, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<EnrollmentSubject>().AddAsync(electiveSubject, cancellationToken);
        }

        public void RemoveElectiveSubject(EnrollmentSubject electiveSubject)
        {
            DbContext.Set<EnrollmentSubject>().Remove(electiveSubject);
        }

        public async Task<IReadOnlyList<StudentDiscount>> GetDiscountsAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var discounts = await DbContext.Set<StudentDiscount>()
                .Where(discount => discount.EnrollmentId == enrollmentId)
                .ToListAsync(cancellationToken);

            return discounts;
        }

        public async Task<StudentDiscount> GetDiscountByIdAsync(Guid discountId, CancellationToken cancellationToken = default)
        {
            var discount = await DbContext.Set<StudentDiscount>()
                .FirstOrDefaultAsync(d => d.Id == discountId, cancellationToken);

            return discount;
        }

        public async Task AddDiscountAsync(StudentDiscount discount, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<StudentDiscount>().AddAsync(discount, cancellationToken);
        }

        public void RemoveDiscount(StudentDiscount discount)
        {
            DbContext.Set<StudentDiscount>().Remove(discount);
        }

        public async Task<IReadOnlyList<AwardSummaryItem>> GetDiscountSummaryAsync(Guid? academicYearId, string discountTypeCode, CancellationToken cancellationToken = default)
        {
            IQueryable<StudentDiscount> discountsQuery = DbContext.Set<StudentDiscount>();

            if (academicYearId.HasValue)
            {
                discountsQuery = discountsQuery.Where(discount => discount.Enrollment.ClassSection.AcademicClass.AcademicYearId == academicYearId.Value);
            }

            if (!string.IsNullOrWhiteSpace(discountTypeCode))
            {
                discountsQuery = discountsQuery.Where(discount => discount.DiscountTypeCode == discountTypeCode);
            }

            var summary = await discountsQuery
                .GroupBy(discount => discount.DiscountTypeCode)
                .Select(group => new AwardSummaryItem { TypeCode = group.Key, StudentCount = group.Count() })
                .OrderBy(item => item.TypeCode)
                .ToListAsync(cancellationToken);

            return summary;
        }

        public async Task<IReadOnlyList<StudentScholarship>> GetScholarshipsAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var scholarships = await DbContext.Set<StudentScholarship>()
                .Where(scholarship => scholarship.EnrollmentId == enrollmentId)
                .ToListAsync(cancellationToken);

            return scholarships;
        }

        public async Task<StudentScholarship> GetScholarshipByIdAsync(Guid scholarshipId, CancellationToken cancellationToken = default)
        {
            var scholarship = await DbContext.Set<StudentScholarship>()
                .FirstOrDefaultAsync(s => s.Id == scholarshipId, cancellationToken);

            return scholarship;
        }

        public async Task AddScholarshipAsync(StudentScholarship scholarship, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<StudentScholarship>().AddAsync(scholarship, cancellationToken);
        }

        public void RemoveScholarship(StudentScholarship scholarship)
        {
            DbContext.Set<StudentScholarship>().Remove(scholarship);
        }

        public async Task<IReadOnlyList<AwardSummaryItem>> GetScholarshipSummaryAsync(Guid? academicYearId, string scholarshipTypeCode, CancellationToken cancellationToken = default)
        {
            IQueryable<StudentScholarship> scholarshipsQuery = DbContext.Set<StudentScholarship>();

            if (academicYearId.HasValue)
            {
                scholarshipsQuery = scholarshipsQuery.Where(scholarship => scholarship.Enrollment.ClassSection.AcademicClass.AcademicYearId == academicYearId.Value);
            }

            if (!string.IsNullOrWhiteSpace(scholarshipTypeCode))
            {
                scholarshipsQuery = scholarshipsQuery.Where(scholarship => scholarship.ScholarshipTypeCode == scholarshipTypeCode);
            }

            var summary = await scholarshipsQuery
                .GroupBy(scholarship => scholarship.ScholarshipTypeCode)
                .Select(group => new AwardSummaryItem { TypeCode = group.Key, StudentCount = group.Count() })
                .OrderBy(item => item.TypeCode)
                .ToListAsync(cancellationToken);

            return summary;
        }

        public async Task<IReadOnlyList<EnrollmentFeeSelection>> GetFeeSelectionsAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var feeSelections = await DbContext.Set<EnrollmentFeeSelection>()
                .Where(selection => selection.EnrollmentId == enrollmentId)
                .ToListAsync(cancellationToken);

            return feeSelections;
        }

        public async Task<EnrollmentFeeSelection> GetFeeSelectionByIdAsync(Guid feeSelectionId, CancellationToken cancellationToken = default)
        {
            var feeSelection = await DbContext.Set<EnrollmentFeeSelection>()
                .FirstOrDefaultAsync(selection => selection.Id == feeSelectionId, cancellationToken);

            return feeSelection;
        }

        public async Task<bool> FeeSelectionExistsAsync(Guid enrollmentId, Guid feeStructureItemId, CancellationToken cancellationToken = default)
        {
            var exists = await DbContext.Set<EnrollmentFeeSelection>()
                .AnyAsync(selection => selection.EnrollmentId == enrollmentId && selection.FeeStructureItemId == feeStructureItemId, cancellationToken);

            return exists;
        }

        public async Task AddFeeSelectionAsync(EnrollmentFeeSelection feeSelection, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<EnrollmentFeeSelection>().AddAsync(feeSelection, cancellationToken);
        }

        public void RemoveFeeSelection(EnrollmentFeeSelection feeSelection)
        {
            DbContext.Set<EnrollmentFeeSelection>().Remove(feeSelection);
        }

        public async Task<bool> FeeSelectionExistsForItemAsync(Guid feeStructureItemId, CancellationToken cancellationToken = default)
        {
            var exists = await DbContext.Set<EnrollmentFeeSelection>()
                .AnyAsync(selection => selection.FeeStructureItemId == feeStructureItemId, cancellationToken);

            return exists;
        }

        public async Task<IReadOnlyList<Enrollment>> GetEnrolledByYearAsync(Guid academicYearId, Guid? academicClassId, Guid? classSectionId, CancellationToken cancellationToken = default)
        {
            IQueryable<Enrollment> enrollmentsQuery = DbSet
                .Include(e => e.Student)
                .Include(e => e.ClassSection)
                    .ThenInclude(section => section.AcademicClass)
                .Where(e => e.Status == EnrollmentStatus.Enrolled
                    && e.ClassSection.AcademicClass.AcademicYearId == academicYearId);

            if (academicClassId.HasValue)
            {
                enrollmentsQuery = enrollmentsQuery.Where(e => e.ClassSection.AcademicClassId == academicClassId.Value);
            }

            if (classSectionId.HasValue)
            {
                enrollmentsQuery = enrollmentsQuery.Where(e => e.ClassSectionId == classSectionId.Value);
            }

            var enrollments = await enrollmentsQuery.ToListAsync(cancellationToken);
            return enrollments;
        }

        public async Task<PagedResult<Enrollment>> SearchEnrolledByStudentAsync(Guid? academicYearId, string search, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var enrollmentsQuery = BuildStudentSearchQuery(academicYearId, search);

            var totalCount = await enrollmentsQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await enrollmentsQuery
                .OrderBy(e => e.Student.FirstName)
                .ThenBy(e => e.Student.LastName)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<Enrollment>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<IReadOnlyList<Enrollment>> SearchEnrolledByStudentAllAsync(Guid? academicYearId, string search, CancellationToken cancellationToken = default)
        {
            var enrollmentsQuery = BuildStudentSearchQuery(academicYearId, search);

            var items = await enrollmentsQuery
                .OrderBy(e => e.Student.FirstName)
                .ThenBy(e => e.Student.LastName)
                .ToListAsync(cancellationToken);

            return items;
        }

        private IQueryable<Enrollment> BuildStudentSearchQuery(Guid? academicYearId, string search)
        {
            IQueryable<Enrollment> enrollmentsQuery = DbSet
                .Include(e => e.Student)
                .Include(e => e.ClassSection)
                    .ThenInclude(section => section.AcademicClass)
                .Where(e => e.Status == EnrollmentStatus.Enrolled);

            if (academicYearId.HasValue)
            {
                enrollmentsQuery = enrollmentsQuery.Where(e => e.ClassSection.AcademicClass.AcademicYearId == academicYearId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerms = search.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in searchTerms)
                {
                    var termPattern = "%" + term + "%";
                    enrollmentsQuery = enrollmentsQuery.Where(e =>
                        EF.Functions.ILike(e.Student.FirstName, termPattern)
                        || EF.Functions.ILike(e.Student.MiddleName, termPattern)
                        || EF.Functions.ILike(e.Student.LastName, termPattern)
                        || EF.Functions.ILike(e.Student.AdmissionNo, termPattern)
                        || EF.Functions.ILike(e.Student.Email, termPattern));
                }
            }

            return enrollmentsQuery;
        }

        public async Task<IReadOnlyList<StudentDiscount>> GetDiscountsByEnrollmentIdsAsync(IReadOnlyList<Guid> enrollmentIds, CancellationToken cancellationToken = default)
        {
            var discounts = await DbContext.Set<StudentDiscount>()
                .Where(discount => enrollmentIds.Contains(discount.EnrollmentId))
                .ToListAsync(cancellationToken);

            return discounts;
        }

        public async Task<IReadOnlyList<StudentScholarship>> GetScholarshipsByEnrollmentIdsAsync(IReadOnlyList<Guid> enrollmentIds, CancellationToken cancellationToken = default)
        {
            var scholarships = await DbContext.Set<StudentScholarship>()
                .Where(scholarship => enrollmentIds.Contains(scholarship.EnrollmentId))
                .ToListAsync(cancellationToken);

            return scholarships;
        }

        public async Task<IReadOnlyList<EnrollmentFeeSelection>> GetFeeSelectionsByEnrollmentIdsAsync(IReadOnlyList<Guid> enrollmentIds, CancellationToken cancellationToken = default)
        {
            var feeSelections = await DbContext.Set<EnrollmentFeeSelection>()
                .Where(selection => enrollmentIds.Contains(selection.EnrollmentId))
                .ToListAsync(cancellationToken);

            return feeSelections;
        }
    }
}
