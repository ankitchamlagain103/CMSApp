using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Enrollments;
using Application.Enrollments.Commands;
using Application.Promotions.Commands;
using Application.Promotions.Dtos;
using Application.Promotions.Validators;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.Promotions
{
    // Reuses IEnrollmentService.CreateEnrollmentAsync for the new enrollment -- same "one
    // Application service injects another feature service" precedent as
    // SalaryCalculatorService -> IEmployeeService -- so promotion gets the exact same
    // capacity/roll-number/one-active-enrollment-per-year validation every other enrollment
    // create goes through, instead of duplicating it here. This means a promotion is NOT one
    // atomic SaveChangesAsync (CreateEnrollmentAsync commits its own save; closing the old
    // enrollment and logging the StudentPromotion row is a second save) -- a deliberate
    // simplification consistent with how this codebase already composes cross-feature service
    // calls elsewhere, not a fully wrapped DB transaction.
    public class PromotionService : IPromotionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEnrollmentService _enrollmentService;
        private readonly CreateStudentPromotionCommandValidator _createValidator;
        private readonly BulkProcessPromotionCommandValidator _bulkValidator;

        public PromotionService(
            IUnitOfWork unitOfWork,
            IEnrollmentService enrollmentService,
            CreateStudentPromotionCommandValidator createValidator,
            BulkProcessPromotionCommandValidator bulkValidator)
        {
            _unitOfWork = unitOfWork;
            _enrollmentService = enrollmentService;
            _createValidator = createValidator;
            _bulkValidator = bulkValidator;
        }

        public async Task<CommonResponse<StudentPromotionDto>> CreateStudentPromotionAsync(CreateStudentPromotionCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<StudentPromotionDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var fromEnrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(command.FromEnrollmentId, cancellationToken);
            if (fromEnrollment == null)
            {
                var notFoundResponse = CommonResponse<StudentPromotionDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + command.FromEnrollmentId + "' was not found.");
                return notFoundResponse;
            }

            var processResult = await ProcessPromotionAsync(fromEnrollment, command.ToClassSectionId, command.RollNumber, command.PromotionType, command.PromotionDate, command.Remarks, cancellationToken);
            if (processResult.Promotion == null)
            {
                var failResponse = CommonResponse<StudentPromotionDto>.Fail(ResponseCodes.ValidationError, processResult.Error);
                return failResponse;
            }

            var promotionWithDetails = await _unitOfWork.StudentPromotions.GetByIdWithDetailsAsync(processResult.Promotion.Id, cancellationToken);
            var promotionDto = PromotionMapper.ToDto(promotionWithDetails);
            var successResponse = CommonResponse<StudentPromotionDto>.Success(promotionDto, "Student promotion recorded successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<StudentPromotionDto>> GetStudentPromotionByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var promotion = await _unitOfWork.StudentPromotions.GetByIdWithDetailsAsync(id, cancellationToken);
            if (promotion == null)
            {
                var notFoundResponse = CommonResponse<StudentPromotionDto>.Fail(ResponseCodes.NotFound, "Student promotion with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var promotionDto = PromotionMapper.ToDto(promotion);
            var successResponse = CommonResponse<StudentPromotionDto>.Success(promotionDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<StudentPromotionDto>>> GetStudentPromotionsAsync(Guid? studentId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var pagedPromotions = await _unitOfWork.StudentPromotions.GetPagedByFilterAsync(studentId, page, pageSize, cancellationToken);

            var promotionDtos = new List<StudentPromotionDto>();
            foreach (var promotion in pagedPromotions.Items)
            {
                var promotionDto = PromotionMapper.ToDto(promotion);
                promotionDtos.Add(promotionDto);
            }

            var paginatedResponse = new PaginatedResponse<StudentPromotionDto>
            {
                Items = promotionDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = pagedPromotions.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<StudentPromotionDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<BulkPromotionResultDto>> BulkProcessPromotionAsync(BulkProcessPromotionCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _bulkValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<BulkPromotionResultDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var examTerm = await _unitOfWork.ExamTerms.GetByIdAsync(command.ExamTermId, cancellationToken);
            if (examTerm == null)
            {
                var examTermNotFoundResponse = CommonResponse<BulkPromotionResultDto>.Fail(ResponseCodes.NotFound, "Exam term with id '" + command.ExamTermId + "' was not found.");
                return examTermNotFoundResponse;
            }

            var fromClassSection = await _unitOfWork.AcademicClasses.GetSectionByIdAsync(command.FromClassSectionId, cancellationToken);
            if (fromClassSection == null)
            {
                var fromSectionNotFoundResponse = CommonResponse<BulkPromotionResultDto>.Fail(ResponseCodes.NotFound, "From class section with id '" + command.FromClassSectionId + "' was not found.");
                return fromSectionNotFoundResponse;
            }

            var promotedToSection = await _unitOfWork.AcademicClasses.GetSectionByIdAsync(command.PromotedToClassSectionId, cancellationToken);
            if (promotedToSection == null)
            {
                var promotedToNotFoundResponse = CommonResponse<BulkPromotionResultDto>.Fail(ResponseCodes.NotFound, "Promoted-to class section with id '" + command.PromotedToClassSectionId + "' was not found.");
                return promotedToNotFoundResponse;
            }

            var retainedToSection = await _unitOfWork.AcademicClasses.GetSectionByIdAsync(command.RetainedToClassSectionId, cancellationToken);
            if (retainedToSection == null)
            {
                var retainedToNotFoundResponse = CommonResponse<BulkPromotionResultDto>.Fail(ResponseCodes.NotFound, "Retained-to class section with id '" + command.RetainedToClassSectionId + "' was not found.");
                return retainedToNotFoundResponse;
            }

            var enrollments = await _unitOfWork.Enrollments.GetEnrolledByYearAsync(fromClassSection.AcademicClass.AcademicYearId, null, command.FromClassSectionId, cancellationToken);

            var resultDto = new BulkPromotionResultDto { ExamTermId = command.ExamTermId, FromClassSectionId = command.FromClassSectionId };

            foreach (var enrollment in enrollments)
            {
                var studentName = enrollment.Student != null ? (enrollment.Student.FirstName + " " + enrollment.Student.LastName) : null;

                var existingResult = await _unitOfWork.ExamTerms.GetResultByEnrollmentAndTermIgnoringFiltersAsync(enrollment.Id, command.ExamTermId, cancellationToken);
                if (existingResult == null || existingResult.IsDeleted)
                {
                    resultDto.Skipped.Add(new BulkPromotionSkipDto { EnrollmentId = enrollment.Id, StudentName = studentName, Reason = "No exam result found for this exam term." });
                    continue;
                }

                if (existingResult.ResultStatus == ResultStatus.Withheld)
                {
                    resultDto.Skipped.Add(new BulkPromotionSkipDto { EnrollmentId = enrollment.Id, StudentName = studentName, Reason = "Result is withheld." });
                    continue;
                }

                Guid targetSectionId;
                PromotionType promotionType;
                if (existingResult.ResultStatus == ResultStatus.Pass)
                {
                    targetSectionId = command.PromotedToClassSectionId;
                    promotionType = PromotionType.Promoted;
                }
                else
                {
                    targetSectionId = command.RetainedToClassSectionId;
                    promotionType = PromotionType.Retained;
                }

                var processResult = await ProcessPromotionAsync(enrollment, targetSectionId, null, promotionType, command.PromotionDate, command.Remarks, cancellationToken);
                if (processResult.Promotion == null)
                {
                    resultDto.Skipped.Add(new BulkPromotionSkipDto { EnrollmentId = enrollment.Id, StudentName = studentName, Reason = processResult.Error });
                    continue;
                }

                if (promotionType == PromotionType.Promoted)
                {
                    resultDto.PromotedCount++;
                }
                else
                {
                    resultDto.RetainedCount++;
                }
            }

            var successResponse = CommonResponse<BulkPromotionResultDto>.Success(resultDto, "Bulk promotion processed.");
            return successResponse;
        }

        private async Task<(StudentPromotion Promotion, string Error)> ProcessPromotionAsync(Enrollment fromEnrollment, Guid toClassSectionId, string rollNumber, PromotionType promotionType, DateTime promotionDate, string remarks, CancellationToken cancellationToken)
        {
            if (fromEnrollment.Status != EnrollmentStatus.Enrolled)
            {
                return (null, "This enrollment is not currently active.");
            }

            var alreadyProcessed = await _unitOfWork.StudentPromotions.HasPromotionFromEnrollmentAsync(fromEnrollment.Id, cancellationToken);
            if (alreadyProcessed)
            {
                return (null, "This enrollment has already been promoted, retained, or transferred.");
            }

            var toClassSection = await _unitOfWork.AcademicClasses.GetSectionByIdAsync(toClassSectionId, cancellationToken);
            if (toClassSection == null)
            {
                return (null, "Destination class section with id '" + toClassSectionId + "' was not found.");
            }

            var createEnrollmentCommand = new CreateEnrollmentCommand
            {
                StudentId = fromEnrollment.StudentId,
                ClassSectionId = toClassSectionId,
                RollNumber = rollNumber,
                EnrollmentDate = promotionDate
            };

            var createEnrollmentResponse = await _enrollmentService.CreateEnrollmentAsync(createEnrollmentCommand, cancellationToken);
            if (createEnrollmentResponse.ResponseCode != ResponseCodes.Success)
            {
                return (null, createEnrollmentResponse.ResponseMessage);
            }

            fromEnrollment.Status = promotionType == PromotionType.Transferred ? EnrollmentStatus.Transferred : EnrollmentStatus.Completed;

            var promotion = new StudentPromotion
            {
                StudentId = fromEnrollment.StudentId,
                FromEnrollmentId = fromEnrollment.Id,
                ToEnrollmentId = createEnrollmentResponse.Data.Id,
                PromotionDate = promotionDate.Date,
                PromotionType = promotionType,
                Remarks = remarks
            };

            await _unitOfWork.StudentPromotions.AddAsync(promotion, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return (promotion, null);
        }

        private static string BuildValidationErrorMessage(ValidationResult validationResult)
        {
            var errorMessages = new List<string>();
            foreach (var error in validationResult.Errors)
            {
                errorMessages.Add(error.ErrorMessage);
            }

            var combinedMessage = string.Join(" ", errorMessages);
            return combinedMessage;
        }
    }
}
