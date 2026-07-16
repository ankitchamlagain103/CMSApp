using Application.Common.Interfaces;
using Application.Common.Models;
using Application.DocumentTemplates;
using Application.Enrollments.Commands;
using Application.Enrollments.Dtos;
using Application.Enrollments.Queries;
using Application.Enrollments.Validators;
using Domain.Common.Filters;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;

namespace Application.Enrollments
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CreateEnrollmentCommandValidator _createValidator;
        private readonly UpdateEnrollmentCommandValidator _updateValidator;
        private readonly AddDiscountCommandValidator _addDiscountValidator;
        private readonly AddScholarshipCommandValidator _addScholarshipValidator;

        public EnrollmentService(
            IUnitOfWork unitOfWork,
            CreateEnrollmentCommandValidator createValidator,
            UpdateEnrollmentCommandValidator updateValidator,
            AddDiscountCommandValidator addDiscountValidator,
            AddScholarshipCommandValidator addScholarshipValidator)
        {
            _unitOfWork = unitOfWork;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _addDiscountValidator = addDiscountValidator;
            _addScholarshipValidator = addScholarshipValidator;
        }

        public async Task<CommonResponse<EnrollmentDto>> CreateEnrollmentAsync(CreateEnrollmentCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var student = await _unitOfWork.Students.GetByIdAsync(command.StudentId, cancellationToken);
            if (student == null)
            {
                var studentNotFoundResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.NotFound, "Student with id '" + command.StudentId + "' was not found.");
                return studentNotFoundResponse;
            }

            // The section carries its AcademicClass, which carries the academic year -- both are
            // needed for the one-active-enrollment-per-year invariant below.
            var classSection = await _unitOfWork.AcademicClasses.GetSectionByIdAsync(command.ClassSectionId, cancellationToken);
            if (classSection == null)
            {
                var sectionNotFoundResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.NotFound, "Class section with id '" + command.ClassSectionId + "' was not found.");
                return sectionNotFoundResponse;
            }

            var enrollmentExists = await _unitOfWork.Enrollments.EnrollmentExistsAsync(command.StudentId, command.ClassSectionId, cancellationToken);
            if (enrollmentExists)
            {
                var duplicateResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.Conflict, "This student is already enrolled in that section (possibly a soft-deleted enrollment).");
                return duplicateResponse;
            }

            // A student can hold only one active (Enrolled) enrollment per academic year --
            // being in two grades/sections of the same year at once is a data error. Moving a
            // student means closing the old enrollment (Transferred/Withdrawn) first.
            var academicYearId = classSection.AcademicClass.AcademicYearId;
            var hasActiveEnrollment = await _unitOfWork.Enrollments.HasActiveEnrollmentInYearAsync(command.StudentId, academicYearId, null, cancellationToken);
            if (hasActiveEnrollment)
            {
                var activeConflictResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.Conflict, "This student already has an active enrollment in this academic year. Close it (Transferred/Withdrawn/Completed) before enrolling again.");
                return activeConflictResponse;
            }

            // Capacity 0 means unlimited; otherwise only actively enrolled students count toward it.
            if (classSection.Capacity > 0)
            {
                var activeEnrollmentCount = await _unitOfWork.Enrollments.CountActiveBySectionAsync(command.ClassSectionId, cancellationToken);
                if (activeEnrollmentCount >= classSection.Capacity)
                {
                    var capacityResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.Conflict, "This section is at full capacity (" + classSection.Capacity + ").");
                    return capacityResponse;
                }
            }

            var trimmedRollNumber = command.RollNumber?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedRollNumber))
            {
                var rollNumberTaken = await _unitOfWork.Enrollments.RollNumberExistsInSectionAsync(command.ClassSectionId, trimmedRollNumber, null, cancellationToken);
                if (rollNumberTaken)
                {
                    var rollNumberResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.Conflict, "Roll number '" + trimmedRollNumber + "' is already taken in this section.");
                    return rollNumberResponse;
                }
            }

            var enrollment = new Enrollment
            {
                StudentId = command.StudentId,
                ClassSectionId = command.ClassSectionId,
                RollNumber = trimmedRollNumber,
                EnrollmentDate = command.EnrollmentDate,
                Status = EnrollmentStatus.Enrolled,
                Student = student,
                ClassSection = classSection
            };

            await _unitOfWork.Enrollments.AddAsync(enrollment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var enrollmentDto = EnrollmentMapper.ToDto(enrollment);
            var successResponse = CommonResponse<EnrollmentDto>.Success(enrollmentDto, "Student enrolled successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<EnrollmentDto>> GetEnrollmentByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(id, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var enrollmentDto = EnrollmentMapper.ToDto(enrollment);
            var successResponse = CommonResponse<EnrollmentDto>.Success(enrollmentDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<EnrollmentDto>>> GetEnrollmentsAsync(GetEnrollmentsQuery query, CancellationToken cancellationToken = default)
        {
            var filter = new EnrollmentFilter
            {
                StudentId = query.StudentId,
                AcademicClassId = query.AcademicClassId,
                ClassSectionId = query.ClassSectionId,
                AcademicYearId = query.AcademicYearId,
                Status = query.Status,
                DateField = query.DateField,
                FromDate = query.FromDate,
                ToDate = query.ToDate
            };

            var pagedEnrollments = await _unitOfWork.Enrollments.GetPagedByFilterAsync(filter, query.Page, query.PageSize, cancellationToken);

            var enrollmentDtos = new List<EnrollmentDto>();
            foreach (var enrollment in pagedEnrollments.Items)
            {
                var enrollmentDto = EnrollmentMapper.ToDto(enrollment);
                enrollmentDtos.Add(enrollmentDto);
            }

            var paginatedResponse = new PaginatedResponse<EnrollmentDto>
            {
                Items = enrollmentDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedEnrollments.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<EnrollmentDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<EnrollmentDto>> UpdateEnrollmentAsync(Guid id, UpdateEnrollmentCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var enrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(id, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            var trimmedRollNumber = command.RollNumber?.Trim();
            var rollNumberIsChanging = !string.Equals(enrollment.RollNumber, trimmedRollNumber, StringComparison.Ordinal);
            if (rollNumberIsChanging && !string.IsNullOrWhiteSpace(trimmedRollNumber))
            {
                var rollNumberTaken = await _unitOfWork.Enrollments.RollNumberExistsInSectionAsync(enrollment.ClassSectionId, trimmedRollNumber, id, cancellationToken);
                if (rollNumberTaken)
                {
                    var rollNumberResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.Conflict, "Roll number '" + trimmedRollNumber + "' is already taken in this section.");
                    return rollNumberResponse;
                }
            }

            // Reactivating a closed enrollment re-runs the one-active-per-year check -- otherwise
            // flipping an old row back to Enrolled would sneak a second active enrollment in.
            var isReactivating = command.Status == EnrollmentStatus.Enrolled && enrollment.Status != EnrollmentStatus.Enrolled;
            if (isReactivating)
            {
                var academicYearId = enrollment.ClassSection.AcademicClass.AcademicYearId;
                var hasActiveEnrollment = await _unitOfWork.Enrollments.HasActiveEnrollmentInYearAsync(enrollment.StudentId, academicYearId, id, cancellationToken);
                if (hasActiveEnrollment)
                {
                    var activeConflictResponse = CommonResponse<EnrollmentDto>.Fail(ResponseCodes.Conflict, "This student already has an active enrollment in this academic year.");
                    return activeConflictResponse;
                }
            }

            enrollment.RollNumber = trimmedRollNumber;
            enrollment.EnrollmentDate = command.EnrollmentDate;
            enrollment.Status = command.Status;

            _unitOfWork.Enrollments.Update(enrollment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var enrollmentDto = EnrollmentMapper.ToDto(enrollment);
            var successResponse = CommonResponse<EnrollmentDto>.Success(enrollmentDto, "Enrollment updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteEnrollmentAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(id, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + id + "' was not found.");
                return notFoundResponse;
            }

            _unitOfWork.Enrollments.Remove(enrollment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Enrollment deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<EnrollmentSubjectDto>> AddElectiveSubjectAsync(Guid enrollmentId, Guid classSubjectId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(enrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<EnrollmentSubjectDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + enrollmentId + "' was not found.");
                return notFoundResponse;
            }

            var classSubject = await _unitOfWork.AcademicClasses.GetClassSubjectByIdAsync(classSubjectId, cancellationToken);
            if (classSubject == null)
            {
                var subjectNotFoundResponse = CommonResponse<EnrollmentSubjectDto>.Fail(ResponseCodes.NotFound, "Class subject with id '" + classSubjectId + "' was not found.");
                return subjectNotFoundResponse;
            }

            // The elective must be offered by the class the student's section belongs to --
            // subjects hang off the class (grade), so every section shares one subject list.
            if (classSubject.AcademicClassId != enrollment.ClassSection.AcademicClassId)
            {
                var wrongClassResponse = CommonResponse<EnrollmentSubjectDto>.Fail(ResponseCodes.ValidationError, "That subject belongs to a different class than this enrollment.");
                return wrongClassResponse;
            }

            // A section-scoped optional subject is only pickable by students of that section.
            if (classSubject.ClassSectionId.HasValue && classSubject.ClassSectionId.Value != enrollment.ClassSectionId)
            {
                var wrongSectionResponse = CommonResponse<EnrollmentSubjectDto>.Fail(ResponseCodes.ValidationError, "Subject '" + classSubject.SubjectCode + "' is only offered in a different section.");
                return wrongSectionResponse;
            }

            // Mandatory subjects apply implicitly -- only electives get an explicit row.
            if (classSubject.IsMandatory)
            {
                var mandatoryResponse = CommonResponse<EnrollmentSubjectDto>.Fail(ResponseCodes.ValidationError, "Subject '" + classSubject.SubjectCode + "' is mandatory and applies automatically.");
                return mandatoryResponse;
            }

            var alreadyAdded = await _unitOfWork.Enrollments.ElectiveSubjectExistsAsync(enrollmentId, classSubjectId, cancellationToken);
            if (alreadyAdded)
            {
                var conflictResponse = CommonResponse<EnrollmentSubjectDto>.Fail(ResponseCodes.Conflict, "This elective is already added to the enrollment.");
                return conflictResponse;
            }

            var electiveSubject = new EnrollmentSubject
            {
                EnrollmentId = enrollmentId,
                ClassSubjectId = classSubjectId,
                ClassSubject = classSubject
            };

            await _unitOfWork.Enrollments.AddElectiveSubjectAsync(electiveSubject, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var electiveSubjectDto = EnrollmentMapper.ToElectiveSubjectDto(electiveSubject);
            var successResponse = CommonResponse<EnrollmentSubjectDto>.Success(electiveSubjectDto, "Elective subject added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveElectiveSubjectAsync(Guid enrollmentId, Guid electiveSubjectId, CancellationToken cancellationToken = default)
        {
            var electiveSubject = await _unitOfWork.Enrollments.GetElectiveSubjectByIdAsync(electiveSubjectId, cancellationToken);
            if (electiveSubject == null || electiveSubject.EnrollmentId != enrollmentId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Elective subject was not found on this enrollment.");
                return notFoundResponse;
            }

            _unitOfWork.Enrollments.RemoveElectiveSubject(electiveSubject);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Elective subject removed successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<EnrollmentSubjectDto>>> GetElectiveSubjectsAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(enrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<List<EnrollmentSubjectDto>>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + enrollmentId + "' was not found.");
                return notFoundResponse;
            }

            var electiveSubjects = await _unitOfWork.Enrollments.GetElectiveSubjectsAsync(enrollmentId, cancellationToken);

            var electiveSubjectDtos = new List<EnrollmentSubjectDto>();
            foreach (var electiveSubject in electiveSubjects)
            {
                var electiveSubjectDto = EnrollmentMapper.ToElectiveSubjectDto(electiveSubject);
                electiveSubjectDtos.Add(electiveSubjectDto);
            }

            var successResponse = CommonResponse<List<EnrollmentSubjectDto>>.Success(electiveSubjectDtos);
            return successResponse;
        }

        public async Task<CommonResponse<StudentDiscountDto>> AddDiscountAsync(Guid enrollmentId, AddDiscountCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _addDiscountValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<StudentDiscountDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(enrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<StudentDiscountDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + enrollmentId + "' was not found.");
                return notFoundResponse;
            }

            var discountTypeCode = command.DiscountTypeCode.Trim();
            var discountTypeConfig = await _unitOfWork.Configs.GetByTypeCodeAndCodeAsync(ConfigTypeCodes.DiscountType, discountTypeCode, cancellationToken);
            if (discountTypeConfig == null)
            {
                var invalidTypeResponse = CommonResponse<StudentDiscountDto>.Fail(ResponseCodes.ValidationError, "DiscountTypeCode '" + discountTypeCode + "' is not a known discount type option.");
                return invalidTypeResponse;
            }

            // ValueType/Value are the individual-override path; when the caller omits both, fall
            // back to the DiscountType catalog's configured default rate (the "global"
            // configuration -- AdditionalValue1 = ValueType, AdditionalValue2 = Value).
            AwardValueType resolvedValueType;
            decimal resolvedValue;
            if (command.ValueType.HasValue && command.Value.HasValue)
            {
                resolvedValueType = command.ValueType.Value;
                resolvedValue = command.Value.Value;
            }
            else
            {
                var defaultAward = ResolveDefaultAward(discountTypeConfig);
                if (defaultAward == null)
                {
                    var noDefaultResponse = CommonResponse<StudentDiscountDto>.Fail(ResponseCodes.ValidationError, "DiscountType '" + discountTypeCode + "' has no configured default rate -- supply ValueType and Value explicitly.");
                    return noDefaultResponse;
                }

                resolvedValueType = defaultAward.Value.ValueType;
                resolvedValue = defaultAward.Value.Value;
            }

            var discount = new StudentDiscount
            {
                EnrollmentId = enrollmentId,
                DiscountTypeCode = discountTypeCode,
                ValueType = resolvedValueType,
                Value = resolvedValue,
                Remarks = command.Remarks?.Trim(),
                Enrollment = enrollment
            };

            await _unitOfWork.Enrollments.AddDiscountAsync(discount, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var discountDto = EnrollmentMapper.ToDiscountDto(discount);
            var successResponse = CommonResponse<StudentDiscountDto>.Success(discountDto, "Discount added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveDiscountAsync(Guid enrollmentId, Guid discountId, CancellationToken cancellationToken = default)
        {
            var discount = await _unitOfWork.Enrollments.GetDiscountByIdAsync(discountId, cancellationToken);
            if (discount == null || discount.EnrollmentId != enrollmentId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Discount was not found on this enrollment.");
                return notFoundResponse;
            }

            _unitOfWork.Enrollments.RemoveDiscount(discount);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Discount removed successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<StudentDiscountDto>>> GetDiscountsAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(enrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<List<StudentDiscountDto>>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + enrollmentId + "' was not found.");
                return notFoundResponse;
            }

            var discounts = await _unitOfWork.Enrollments.GetDiscountsAsync(enrollmentId, cancellationToken);

            var discountDtos = new List<StudentDiscountDto>();
            foreach (var discount in discounts)
            {
                var discountDto = EnrollmentMapper.ToDiscountDto(discount);
                discountDtos.Add(discountDto);
            }

            var successResponse = CommonResponse<List<StudentDiscountDto>>.Success(discountDtos);
            return successResponse;
        }

        public async Task<CommonResponse<List<AwardSummaryDto>>> GetDiscountSummaryAsync(Guid? academicYearId, string discountTypeCode, CancellationToken cancellationToken = default)
        {
            var summaryItems = await _unitOfWork.Enrollments.GetDiscountSummaryAsync(academicYearId, discountTypeCode, cancellationToken);

            var summaryDtos = new List<AwardSummaryDto>();
            foreach (var summaryItem in summaryItems)
            {
                var summaryDto = EnrollmentMapper.ToAwardSummaryDto(summaryItem);
                summaryDtos.Add(summaryDto);
            }

            var successResponse = CommonResponse<List<AwardSummaryDto>>.Success(summaryDtos);
            return successResponse;
        }

        public async Task<CommonResponse<StudentScholarshipDto>> AddScholarshipAsync(Guid enrollmentId, AddScholarshipCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _addScholarshipValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<StudentScholarshipDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(enrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<StudentScholarshipDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + enrollmentId + "' was not found.");
                return notFoundResponse;
            }

            var scholarshipTypeCode = command.ScholarshipTypeCode.Trim();
            var scholarshipTypeConfig = await _unitOfWork.Configs.GetByTypeCodeAndCodeAsync(ConfigTypeCodes.ScholarshipType, scholarshipTypeCode, cancellationToken);
            if (scholarshipTypeConfig == null)
            {
                var invalidTypeResponse = CommonResponse<StudentScholarshipDto>.Fail(ResponseCodes.ValidationError, "ScholarshipTypeCode '" + scholarshipTypeCode + "' is not a known scholarship type option.");
                return invalidTypeResponse;
            }

            // ValueType/Value are the individual-override path; when the caller omits both, fall
            // back to the ScholarshipType catalog's configured default rate (the "global"
            // configuration -- AdditionalValue1 = ValueType, AdditionalValue2 = Value).
            AwardValueType resolvedValueType;
            decimal resolvedValue;
            if (command.ValueType.HasValue && command.Value.HasValue)
            {
                resolvedValueType = command.ValueType.Value;
                resolvedValue = command.Value.Value;
            }
            else
            {
                var defaultAward = ResolveDefaultAward(scholarshipTypeConfig);
                if (defaultAward == null)
                {
                    var noDefaultResponse = CommonResponse<StudentScholarshipDto>.Fail(ResponseCodes.ValidationError, "ScholarshipType '" + scholarshipTypeCode + "' has no configured default rate -- supply ValueType and Value explicitly.");
                    return noDefaultResponse;
                }

                resolvedValueType = defaultAward.Value.ValueType;
                resolvedValue = defaultAward.Value.Value;
            }

            var scholarship = new StudentScholarship
            {
                EnrollmentId = enrollmentId,
                ScholarshipTypeCode = scholarshipTypeCode,
                ValueType = resolvedValueType,
                Value = resolvedValue,
                Remarks = command.Remarks?.Trim(),
                Enrollment = enrollment
            };

            await _unitOfWork.Enrollments.AddScholarshipAsync(scholarship, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var scholarshipDto = EnrollmentMapper.ToScholarshipDto(scholarship);
            var successResponse = CommonResponse<StudentScholarshipDto>.Success(scholarshipDto, "Scholarship added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveScholarshipAsync(Guid enrollmentId, Guid scholarshipId, CancellationToken cancellationToken = default)
        {
            var scholarship = await _unitOfWork.Enrollments.GetScholarshipByIdAsync(scholarshipId, cancellationToken);
            if (scholarship == null || scholarship.EnrollmentId != enrollmentId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Scholarship was not found on this enrollment.");
                return notFoundResponse;
            }

            _unitOfWork.Enrollments.RemoveScholarship(scholarship);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Scholarship removed successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<StudentScholarshipDto>>> GetScholarshipsAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(enrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<List<StudentScholarshipDto>>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + enrollmentId + "' was not found.");
                return notFoundResponse;
            }

            var scholarships = await _unitOfWork.Enrollments.GetScholarshipsAsync(enrollmentId, cancellationToken);

            var scholarshipDtos = new List<StudentScholarshipDto>();
            foreach (var scholarship in scholarships)
            {
                var scholarshipDto = EnrollmentMapper.ToScholarshipDto(scholarship);
                scholarshipDtos.Add(scholarshipDto);
            }

            var successResponse = CommonResponse<List<StudentScholarshipDto>>.Success(scholarshipDtos);
            return successResponse;
        }

        public async Task<CommonResponse<List<AwardSummaryDto>>> GetScholarshipSummaryAsync(Guid? academicYearId, string scholarshipTypeCode, CancellationToken cancellationToken = default)
        {
            var summaryItems = await _unitOfWork.Enrollments.GetScholarshipSummaryAsync(academicYearId, scholarshipTypeCode, cancellationToken);

            var summaryDtos = new List<AwardSummaryDto>();
            foreach (var summaryItem in summaryItems)
            {
                var summaryDto = EnrollmentMapper.ToAwardSummaryDto(summaryItem);
                summaryDtos.Add(summaryDto);
            }

            var successResponse = CommonResponse<List<AwardSummaryDto>>.Success(summaryDtos);
            return successResponse;
        }

        public async Task<CommonResponse<EnrollmentFeeSelectionDto>> AddFeeSelectionAsync(Guid enrollmentId, Guid feeStructureItemId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(enrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<EnrollmentFeeSelectionDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + enrollmentId + "' was not found.");
                return notFoundResponse;
            }

            // The item must be an OPTIONAL fee item on the enrollment's own class -- selecting a
            // mandatory item (which already applies to everyone) or one the class doesn't even
            // charge makes no sense.
            var classFeeStructure = await _unitOfWork.FeeStructures.GetByAcademicClassIdAsync(enrollment.ClassSection.AcademicClassId, cancellationToken);
            FeeStructureItem matchingItem = null;
            if (classFeeStructure != null)
            {
                foreach (var item in classFeeStructure.Items)
                {
                    if (item.Id == feeStructureItemId)
                    {
                        matchingItem = item;
                    }
                }
            }

            if (matchingItem == null)
            {
                var notChargedResponse = CommonResponse<EnrollmentFeeSelectionDto>.Fail(ResponseCodes.ValidationError, "Fee item '" + feeStructureItemId + "' is not charged on this enrollment's class.");
                return notChargedResponse;
            }

            if (!matchingItem.IsOptional)
            {
                var notOptionalResponse = CommonResponse<EnrollmentFeeSelectionDto>.Fail(ResponseCodes.ValidationError, "Fee item '" + matchingItem.FeeCategoryCode + "' is a mandatory fee -- it already applies automatically.");
                return notOptionalResponse;
            }

            var alreadySelected = await _unitOfWork.Enrollments.FeeSelectionExistsAsync(enrollmentId, feeStructureItemId, cancellationToken);
            if (alreadySelected)
            {
                var conflictResponse = CommonResponse<EnrollmentFeeSelectionDto>.Fail(ResponseCodes.Conflict, "This fee item is already selected on the enrollment.");
                return conflictResponse;
            }

            var feeSelection = new EnrollmentFeeSelection
            {
                EnrollmentId = enrollmentId,
                FeeStructureItemId = feeStructureItemId,
                Enrollment = enrollment
            };

            await _unitOfWork.Enrollments.AddFeeSelectionAsync(feeSelection, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var feeSelectionDto = EnrollmentMapper.ToFeeSelectionDto(feeSelection);
            var successResponse = CommonResponse<EnrollmentFeeSelectionDto>.Success(feeSelectionDto, "Fee selection added successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveFeeSelectionAsync(Guid enrollmentId, Guid feeSelectionId, CancellationToken cancellationToken = default)
        {
            var feeSelection = await _unitOfWork.Enrollments.GetFeeSelectionByIdAsync(feeSelectionId, cancellationToken);
            if (feeSelection == null || feeSelection.EnrollmentId != enrollmentId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Fee selection was not found on this enrollment.");
                return notFoundResponse;
            }

            _unitOfWork.Enrollments.RemoveFeeSelection(feeSelection);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Fee selection removed successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<EnrollmentFeeSelectionDto>>> GetFeeSelectionsAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetByIdAsync(enrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<List<EnrollmentFeeSelectionDto>>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + enrollmentId + "' was not found.");
                return notFoundResponse;
            }

            var feeSelections = await _unitOfWork.Enrollments.GetFeeSelectionsAsync(enrollmentId, cancellationToken);

            var feeSelectionDtos = new List<EnrollmentFeeSelectionDto>();
            foreach (var feeSelection in feeSelections)
            {
                var feeSelectionDto = EnrollmentMapper.ToFeeSelectionDto(feeSelection);
                feeSelectionDtos.Add(feeSelectionDto);
            }

            var successResponse = CommonResponse<List<EnrollmentFeeSelectionDto>>.Success(feeSelectionDtos);
            return successResponse;
        }

        // The student-facing "what does this enrollment owe" view -- bound to the academic year
        // through the enrollment's own class chain, so no separate year parameter is needed.
        public async Task<CommonResponse<EnrollmentFeeStructureDto>> GetFeeStructureAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var enrollment = await _unitOfWork.Enrollments.GetWithDetailsAsync(enrollmentId, cancellationToken);
            if (enrollment == null)
            {
                var notFoundResponse = CommonResponse<EnrollmentFeeStructureDto>.Fail(ResponseCodes.NotFound, "Enrollment with id '" + enrollmentId + "' was not found.");
                return notFoundResponse;
            }

            var academicClass = enrollment.ClassSection.AcademicClass;
            var classFeeStructure = await _unitOfWork.FeeStructures.GetByAcademicClassIdAsync(academicClass.Id, cancellationToken);
            var feeSelections = await _unitOfWork.Enrollments.GetFeeSelectionsAsync(enrollmentId, cancellationToken);

            var selectedItemIds = new List<Guid>();
            foreach (var feeSelection in feeSelections)
            {
                selectedItemIds.Add(feeSelection.FeeStructureItemId);
            }

            var feeItemDtos = new List<FeeLineItemDto>();
            if (classFeeStructure != null)
            {
                foreach (var item in classFeeStructure.Items)
                {
                    var applies = !item.IsOptional || selectedItemIds.Contains(item.Id);

                    var feeItemDto = new FeeLineItemDto
                    {
                        FeeStructureItemId = item.Id,
                        FeeCategoryCode = item.FeeCategoryCode,
                        Amount = item.Amount,
                        FrequencyType = item.FrequencyType,
                        IsOptional = item.IsOptional,
                        IsRefundable = item.IsRefundable,
                        Applies = applies
                    };
                    feeItemDtos.Add(feeItemDto);
                }
            }

            var discounts = await _unitOfWork.Enrollments.GetDiscountsAsync(enrollmentId, cancellationToken);
            var scholarships = await _unitOfWork.Enrollments.GetScholarshipsAsync(enrollmentId, cancellationToken);

            var discountDtos = new List<StudentDiscountDto>();
            foreach (var discount in discounts)
            {
                var discountDto = EnrollmentMapper.ToDiscountDto(discount);
                discountDtos.Add(discountDto);
            }

            var scholarshipDtos = new List<StudentScholarshipDto>();
            foreach (var scholarship in scholarships)
            {
                var scholarshipDto = EnrollmentMapper.ToScholarshipDto(scholarship);
                scholarshipDtos.Add(scholarshipDto);
            }

            var summary = BuildFeeStructureSummary(feeItemDtos, discounts, scholarships);

            var feeStructureDto = new EnrollmentFeeStructureDto
            {
                EnrollmentId = enrollmentId,
                AcademicYearId = academicClass.AcademicYearId,
                AcademicClassId = academicClass.Id,
                GradeCode = academicClass.GradeCode,
                FeeItems = feeItemDtos,
                Discounts = discountDtos,
                Scholarships = scholarshipDtos,
                Summary = summary
            };

            var successResponse = CommonResponse<EnrollmentFeeStructureDto>.Success(feeStructureDto);
            return successResponse;
        }

        public async Task<CommonResponse<DocumentPreviewDto>> GetFeeReceiptPreviewAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var enrollmentResponse = await GetEnrollmentByIdAsync(enrollmentId, cancellationToken);
            if (enrollmentResponse.Data == null)
            {
                var enrollmentFailureResponse = CommonResponse<DocumentPreviewDto>.Fail(enrollmentResponse.ResponseCode, enrollmentResponse.ResponseMessage);
                return enrollmentFailureResponse;
            }

            var feeStructureResponse = await GetFeeStructureAsync(enrollmentId, cancellationToken);
            if (feeStructureResponse.Data == null)
            {
                var feeStructureFailureResponse = CommonResponse<DocumentPreviewDto>.Fail(feeStructureResponse.ResponseCode, feeStructureResponse.ResponseMessage);
                return feeStructureFailureResponse;
            }

            var documentTemplate = await _unitOfWork.DocumentTemplates.GetByTemplateTypeAsync(DocumentTemplateType.FeeReceipt, cancellationToken);
            if (documentTemplate == null)
            {
                var noTemplateResponse = CommonResponse<DocumentPreviewDto>.Fail(ResponseCodes.NotFound, "No document template is configured for '" + DocumentTemplateType.FeeReceipt + "' yet.");
                return noTemplateResponse;
            }

            var enrollmentDto = enrollmentResponse.Data;
            var feeStructureDto = feeStructureResponse.Data;
            var studentName = enrollmentDto.StudentFirstName + " " + enrollmentDto.StudentLastName;

            var placeholderValues = new Dictionary<string, string>
            {
                { "StudentName", studentName },
                { "AdmissionNo", enrollmentDto.StudentAdmissionNo },
                { "GradeCode", enrollmentDto.GradeCode },
                { "SectionCode", enrollmentDto.SectionCode },
                { "RollNumber", enrollmentDto.RollNumber.ToString() },
                { "FeeItemsRows", BuildFeeItemsRows(feeStructureDto.FeeItems) },
                { "DiscountsRows", BuildDiscountsRows(feeStructureDto.Discounts) },
                { "ScholarshipsRows", BuildScholarshipsRows(feeStructureDto.Scholarships) },
                { "MonthlyRecurringTotal", feeStructureDto.Summary.MonthlyRecurringTotal.ToString("F2") },
                { "AnnualTotal", feeStructureDto.Summary.AnnualTotal.ToString("F2") },
                { "OneTimeTotal", feeStructureDto.Summary.OneTimeTotal.ToString("F2") },
                { "RefundableDepositTotal", feeStructureDto.Summary.RefundableDepositTotal.ToString("F2") },
                { "TotalDiscountReduction", feeStructureDto.Summary.TotalDiscountReduction.ToString("F2") },
                { "TotalScholarshipReduction", feeStructureDto.Summary.TotalScholarshipReduction.ToString("F2") },
                { "NetMonthlyRecurring", feeStructureDto.Summary.NetMonthlyRecurring.ToString("F2") }
            };

            var renderedHtml = TemplateRenderer.Render(documentTemplate.HtmlContent, placeholderValues);

            var documentPreviewDto = new DocumentPreviewDto
            {
                TemplateType = DocumentTemplateType.FeeReceipt,
                Html = renderedHtml
            };

            var previewSuccessResponse = CommonResponse<DocumentPreviewDto>.Success(documentPreviewDto);
            return previewSuccessResponse;
        }

        private static string BuildFeeItemsRows(List<FeeLineItemDto> feeItems)
        {
            var rowsHtml = string.Empty;
            foreach (var feeItem in feeItems)
            {
                rowsHtml += "<tr><td>" + feeItem.FeeCategoryCode + "</td><td>" + feeItem.Amount.ToString("F2") + "</td><td>" + feeItem.FrequencyType + "</td><td>" + feeItem.Applies + "</td></tr>";
            }

            return rowsHtml;
        }

        private static string BuildDiscountsRows(List<StudentDiscountDto> discounts)
        {
            var rowsHtml = string.Empty;
            foreach (var discount in discounts)
            {
                rowsHtml += "<tr><td>" + discount.DiscountTypeCode + "</td><td>" + discount.Value.ToString("F2") + " (" + discount.ValueType + ")</td></tr>";
            }

            return rowsHtml;
        }

        private static string BuildScholarshipsRows(List<StudentScholarshipDto> scholarships)
        {
            var rowsHtml = string.Empty;
            foreach (var scholarship in scholarships)
            {
                rowsHtml += "<tr><td>" + scholarship.ScholarshipTypeCode + "</td><td>" + scholarship.Value.ToString("F2") + " (" + scholarship.ValueType + ")</td></tr>";
            }

            return rowsHtml;
        }

        // Discounts/scholarships reduce only MonthlyRecurringTotal (the ongoing tuition-style
        // cost) -- Annual/OneTime items (admission, deposit, exam, tour, ...) are priced but not
        // discounted in this pass, since a single % applied across mixed billing horizons doesn't
        // have one sensible meaning. Percentage awards are computed against the pre-discount
        // monthly total (additive across multiple awards, not compounded); fixed-amount awards
        // subtract directly. Net is floored at 0.
        private static FeeStructureSummaryDto BuildFeeStructureSummary(List<FeeLineItemDto> feeItems, IReadOnlyList<StudentDiscount> discounts, IReadOnlyList<StudentScholarship> scholarships)
        {
            decimal monthlyTotal = 0m;
            decimal annualTotal = 0m;
            decimal oneTimeTotal = 0m;
            decimal refundableTotal = 0m;

            foreach (var feeItem in feeItems)
            {
                if (!feeItem.Applies)
                {
                    continue;
                }

                if (feeItem.IsRefundable)
                {
                    refundableTotal += feeItem.Amount;
                    continue;
                }

                if (feeItem.FrequencyType == FeeFrequencyType.Monthly)
                {
                    monthlyTotal += feeItem.Amount;
                }
                else if (feeItem.FrequencyType == FeeFrequencyType.Annual)
                {
                    annualTotal += feeItem.Amount;
                }
                else
                {
                    oneTimeTotal += feeItem.Amount;
                }
            }

            decimal discountReduction = 0m;
            foreach (var discount in discounts)
            {
                discountReduction += discount.ValueType == AwardValueType.Percentage
                    ? monthlyTotal * (discount.Value / 100m)
                    : discount.Value;
            }

            decimal scholarshipReduction = 0m;
            foreach (var scholarship in scholarships)
            {
                scholarshipReduction += scholarship.ValueType == AwardValueType.Percentage
                    ? monthlyTotal * (scholarship.Value / 100m)
                    : scholarship.Value;
            }

            var summary = new FeeStructureSummaryDto
            {
                MonthlyRecurringTotal = monthlyTotal,
                AnnualTotal = annualTotal,
                OneTimeTotal = oneTimeTotal,
                RefundableDepositTotal = refundableTotal,
                TotalDiscountReduction = discountReduction,
                TotalScholarshipReduction = scholarshipReduction,
                NetMonthlyRecurring = Math.Max(0m, monthlyTotal - discountReduction - scholarshipReduction)
            };

            return summary;
        }

        // The "global" half of the two-tier discount/scholarship configuration: a DiscountType/
        // ScholarshipType Config row optionally carries a default rate in its free-form
        // AdditionalValue slots (AdditionalValue1 = ValueType name, AdditionalValue2 = Value) --
        // same reuse of Config's extensibility as the Subject catalog's short-name/credit/category
        // convention. Returns null if either slot is missing or unparseable, meaning "no default
        // configured for this type."
        private static (AwardValueType ValueType, decimal Value)? ResolveDefaultAward(Config typeConfig)
        {
            if (string.IsNullOrWhiteSpace(typeConfig.AdditionalValue1) || string.IsNullOrWhiteSpace(typeConfig.AdditionalValue2))
            {
                return null;
            }

            if (!Enum.TryParse<AwardValueType>(typeConfig.AdditionalValue1, true, out var valueType))
            {
                return null;
            }

            if (!decimal.TryParse(typeConfig.AdditionalValue2, out var value))
            {
                return null;
            }

            return (valueType, value);
        }

        private static string BuildValidationErrorMessage(ValidationResult validationResult)
        {
            var errorMessages = new List<string>();
            foreach (var failure in validationResult.Errors)
            {
                errorMessages.Add(failure.ErrorMessage);
            }

            var combinedMessage = string.Join(" ", errorMessages);
            return combinedMessage;
        }
    }
}
