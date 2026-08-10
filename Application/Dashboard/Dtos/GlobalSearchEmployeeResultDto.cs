using Domain.Enums;

namespace Application.Dashboard.Dtos
{
    // One matched Employee row in a global search result. Covers teaching staff too -- there is
    // no separate Teacher root aggregate in this codebase (removed 2026-08-06, folded into
    // Employee) -- IsTeacher (EmployeeRoleHelper.IsTeachingStaff) just flags whether this
    // employee is teaching staff, for UI badging.
    public class GlobalSearchEmployeeResultDto
    {
        public Guid Id { get; set; }
        public string EmployeeCode { get; set; }
        public string FullName { get; set; }
        public string JobPositionCode { get; set; }
        public string JobPositionLabel { get; set; }
        public EmploymentStatus EmploymentStatus { get; set; }
        public bool IsTeacher { get; set; }
    }
}
