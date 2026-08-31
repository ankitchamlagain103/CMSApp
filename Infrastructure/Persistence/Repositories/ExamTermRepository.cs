using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class ExamTermRepository : Repository<ExamTerm, Guid>, IExamTermRepository
    {
        public ExamTermRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<ExamTerm>> GetPagedByFilterAsync(Guid? academicYearId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<ExamTerm> termsQuery = DbSet;

            if (academicYearId.HasValue)
            {
                termsQuery = termsQuery.Where(term => term.AcademicYearId == academicYearId.Value);
            }

            var totalCount = await termsQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await termsQuery
                .OrderBy(term => term.Sequence)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<ExamTerm>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<bool> CodeExistsAsync(string code, Guid? excludeExamTermId, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the unique index still sees soft-deleted rows.
            IQueryable<ExamTerm> query = DbSet
                .IgnoreQueryFilters()
                .Where(term => term.Code == code);

            if (excludeExamTermId.HasValue)
            {
                query = query.Where(term => term.Id != excludeExamTermId.Value);
            }

            var exists = await query.AnyAsync(cancellationToken);
            return exists;
        }

        public async Task<bool> HasExamsAsync(Guid examTermId, CancellationToken cancellationToken = default)
        {
            var hasExams = await DbContext.Set<Exam>()
                .AnyAsync(exam => exam.ExamTermId == examTermId, cancellationToken);

            return hasExams;
        }

        public async Task<Exam> GetExamByIdAsync(Guid examId, CancellationToken cancellationToken = default)
        {
            var exam = await DbContext.Set<Exam>()
                .Include(e => e.ExamTerm)
                .Include(e => e.ClassSubject)
                    .ThenInclude(cs => cs.AcademicClass)
                .Include(e => e.TimePeriod)
                .FirstOrDefaultAsync(e => e.Id == examId, cancellationToken);

            return exam;
        }

        public async Task<IReadOnlyList<Exam>> GetExamsAsync(Guid? examTermId, Guid? classSubjectId, CancellationToken cancellationToken = default)
        {
            IQueryable<Exam> examsQuery = DbContext.Set<Exam>()
                .Include(e => e.ClassSubject)
                    .ThenInclude(cs => cs.AcademicClass)
                .Include(e => e.TimePeriod);

            if (examTermId.HasValue)
            {
                examsQuery = examsQuery.Where(e => e.ExamTermId == examTermId.Value);
            }

            if (classSubjectId.HasValue)
            {
                examsQuery = examsQuery.Where(e => e.ClassSubjectId == classSubjectId.Value);
            }

            var exams = await examsQuery
                .OrderBy(e => e.ExamDate)
                .ThenBy(e => e.StartTime)
                .ToListAsync(cancellationToken);

            return exams;
        }

        public async Task<bool> ExamExistsAsync(Guid examTermId, Guid classSubjectId, Guid? excludeExamId, CancellationToken cancellationToken = default)
        {
            IQueryable<Exam> query = DbContext.Set<Exam>()
                .Where(e => e.ExamTermId == examTermId && e.ClassSubjectId == classSubjectId);

            if (excludeExamId.HasValue)
            {
                query = query.Where(e => e.Id != excludeExamId.Value);
            }

            var exists = await query.AnyAsync(cancellationToken);
            return exists;
        }

        public async Task AddExamAsync(Exam exam, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<Exam>().AddAsync(exam, cancellationToken);
        }

        public void RemoveExam(Exam exam)
        {
            DbContext.Set<Exam>().Remove(exam);
        }

        // --- Marks Entry & Result Processing (Phase 2) ---

        public async Task<bool> HasMarksAsync(Guid examId, CancellationToken cancellationToken = default)
        {
            var hasMarks = await DbContext.Set<StudentExamMark>()
                .AnyAsync(mark => mark.ExamId == examId, cancellationToken);

            return hasMarks;
        }

        public async Task<StudentExamMark> GetMarkByIdAsync(Guid markId, CancellationToken cancellationToken = default)
        {
            var mark = await DbContext.Set<StudentExamMark>()
                .Include(m => m.Exam)
                    .ThenInclude(e => e.ClassSubject)
                .Include(m => m.Enrollment)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(m => m.Id == markId, cancellationToken);

            return mark;
        }

        public async Task<StudentExamMark> GetMarkByExamAndEnrollmentAsync(Guid examId, Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var mark = await DbContext.Set<StudentExamMark>()
                .FirstOrDefaultAsync(m => m.ExamId == examId && m.EnrollmentId == enrollmentId, cancellationToken);

            return mark;
        }

        public async Task<IReadOnlyList<StudentExamMark>> GetMarksAsync(Guid? examId, Guid? enrollmentId, Guid? classSectionId, CancellationToken cancellationToken = default)
        {
            IQueryable<StudentExamMark> marksQuery = DbContext.Set<StudentExamMark>()
                .Include(m => m.Exam)
                    .ThenInclude(e => e.ClassSubject)
                .Include(m => m.Enrollment)
                    .ThenInclude(e => e.Student);

            if (examId.HasValue)
            {
                marksQuery = marksQuery.Where(m => m.ExamId == examId.Value);
            }

            if (enrollmentId.HasValue)
            {
                marksQuery = marksQuery.Where(m => m.EnrollmentId == enrollmentId.Value);
            }

            if (classSectionId.HasValue)
            {
                marksQuery = marksQuery.Where(m => m.Enrollment.ClassSectionId == classSectionId.Value);
            }

            var marks = await marksQuery.ToListAsync(cancellationToken);
            return marks;
        }

        public async Task<IReadOnlyList<StudentExamMark>> GetMarksByExamIdsAsync(IReadOnlyList<Guid> examIds, CancellationToken cancellationToken = default)
        {
            var marks = await DbContext.Set<StudentExamMark>()
                .Where(m => examIds.Contains(m.ExamId))
                .ToListAsync(cancellationToken);

            return marks;
        }

        public async Task AddMarkAsync(StudentExamMark mark, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<StudentExamMark>().AddAsync(mark, cancellationToken);
        }

        public void RemoveMark(StudentExamMark mark)
        {
            DbContext.Set<StudentExamMark>().Remove(mark);
        }

        public async Task<IReadOnlyList<Exam>> GetExamsByTermAsync(Guid examTermId, Guid? academicClassId, CancellationToken cancellationToken = default)
        {
            IQueryable<Exam> examsQuery = DbContext.Set<Exam>()
                .Include(e => e.ClassSubject)
                .Where(e => e.ExamTermId == examTermId);

            if (academicClassId.HasValue)
            {
                examsQuery = examsQuery.Where(e => e.ClassSubject.AcademicClassId == academicClassId.Value);
            }

            var exams = await examsQuery.ToListAsync(cancellationToken);
            return exams;
        }

        public async Task<StudentResult> GetResultByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await DbContext.Set<StudentResult>()
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.Student)
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.ClassSection)
                        .ThenInclude(cs => cs.AcademicClass)
                .Include(r => r.ExamTerm)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            return result;
        }

        public async Task<StudentResult> GetResultByEnrollmentAndTermIgnoringFiltersAsync(Guid enrollmentId, Guid examTermId, CancellationToken cancellationToken = default)
        {
            var result = await DbContext.Set<StudentResult>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.EnrollmentId == enrollmentId && r.ExamTermId == examTermId, cancellationToken);

            return result;
        }

        public async Task<PagedResult<StudentResult>> GetResultsPagedByFilterAsync(Guid? examTermId, Guid? classSectionId, Guid? enrollmentId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<StudentResult> resultsQuery = DbContext.Set<StudentResult>()
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.Student)
                .Include(r => r.Enrollment)
                    .ThenInclude(e => e.ClassSection)
                        .ThenInclude(cs => cs.AcademicClass);

            if (examTermId.HasValue)
            {
                resultsQuery = resultsQuery.Where(r => r.ExamTermId == examTermId.Value);
            }

            if (classSectionId.HasValue)
            {
                resultsQuery = resultsQuery.Where(r => r.Enrollment.ClassSectionId == classSectionId.Value);
            }

            if (enrollmentId.HasValue)
            {
                resultsQuery = resultsQuery.Where(r => r.EnrollmentId == enrollmentId.Value);
            }

            var totalCount = await resultsQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await resultsQuery
                .OrderBy(r => r.Rank ?? int.MaxValue)
                .ThenByDescending(r => r.Percentage)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<StudentResult>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<IReadOnlyList<StudentResult>> GetResultsForRankingAsync(Guid examTermId, Guid classSectionId, CancellationToken cancellationToken = default)
        {
            var results = await DbContext.Set<StudentResult>()
                .Where(r => r.ExamTermId == examTermId && r.Enrollment.ClassSectionId == classSectionId)
                .ToListAsync(cancellationToken);

            return results;
        }

        public async Task<IReadOnlyList<StudentResult>> GetResultsByTermAsync(Guid examTermId, CancellationToken cancellationToken = default)
        {
            var results = await DbContext.Set<StudentResult>()
                .Where(r => r.ExamTermId == examTermId)
                .ToListAsync(cancellationToken);

            return results;
        }

        public async Task AddResultAsync(StudentResult result, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<StudentResult>().AddAsync(result, cancellationToken);
        }

        public void RemoveResult(StudentResult result)
        {
            DbContext.Set<StudentResult>().Remove(result);
        }
    }
}
