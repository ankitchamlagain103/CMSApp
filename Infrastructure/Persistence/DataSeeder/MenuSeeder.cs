using Domain.Constants;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.DataSeeder
{
    // Seeds the full menu/permission catalog and grants every endpoint-backed menu to the
    // SuperAdmin role. The catalog is SYNCED, not just created: an existing row whose
    // hierarchy, route, url, or visibility drifted from the definition below is updated back
    // in place on the next startup, and a soft-deleted catalog row is resurrected (its Code
    // stays reserved by the unique index either way). Hand edits to these rows do not
    // survive a restart -- the catalog is structural and owned by this file.
    //
    // Tree shape:
    //   MAIN_MENU  -- a nav area; no controller/action.
    //   SUB_MENU   -- a feature's list page; visible, carries the list endpoint AND the
    //                 frontend Url, so it doubles as the permission for that endpoint
    //                 (AuthorizedAction matches on Controller/Action only, never MenuType).
    //   PERMISSION -- hidden authorization record for every other endpoint.
    //
    // When adding a new controller/action, remember Controller must be the route value
    // ("Users" for UsersController), not the class name.
    public static class MenuSeeder
    {
        private sealed class MenuSeedDefinition
        {
            public string Code { get; set; }
            public string DisplayName { get; set; }
            public string Url { get; set; }
            public string Icon { get; set; }
            public string MenuType { get; set; }
            public string Controller { get; set; }
            public string Action { get; set; }
            public string ParentCode { get; set; }
            public int Order { get; set; }
            public bool IsHidden { get; set; }
            public bool IsQuickLink { get; set; }
            public bool IsDashboardWidget { get; set; }
        }

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            await SeedMenusAsync(dbContext);
            await SeedSuperAdminRoleClaimsAsync(dbContext, roleManager);
        }

        private static List<MenuSeedDefinition> BuildMenuCatalog()
        {
            var catalog = new List<MenuSeedDefinition>();

            catalog.Add(MainMenu("DASHBOARD", "Dashboard", "icons.DashboardOutlined", 1, "/dashboard/analytics"));
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_SUMMARY", "View Dashboard Summary", "Dashboard", "GetSummary", 4, isDashboardWidget: true));
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_ENROLLMENT_STATS", "View Enrollment Stats", "Dashboard", "GetEnrollmentStats", 5));
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_TEACHER_WIDGET", "View Teacher List Widget", "Dashboard", "GetTeacherListWidget", 6));
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_USER_WIDGET", "View User List Widget", "Dashboard", "GetUserListWidget", 7));
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_BAR_GRAPH", "View Dashboard Bar Graph", "Dashboard", "GetBarGraph", 8));
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_CURRENT_ACADEMIC_YEAR", "View Current Academic Year", "Dashboard", "GetCurrentAcademicYear", 9));
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_QUICK_MENUS", "View Quick Menu Suggestions", "Dashboard", "GetQuickMenus", 10));
            // Accounts/HR dashboard summaries (2026-07-28) -- persona-oriented composite widgets,
            // same shape as DASHBOARD_SUMMARY. Grant to whatever role an admin creates for that
            // function (e.g. "Accounts"/"HR") via POST /api/roles/claims -- no hardcoded role names.
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_ACCOUNTS_SUMMARY", "View Accounts Dashboard Summary", "Dashboard", "GetAccountsSummary", 11, isDashboardWidget: true));
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_HR_SUMMARY", "View HR Dashboard Summary", "Dashboard", "GetHrSummary", 12, isDashboardWidget: true));
            // Navbar "Ctrl+K"-style global search across Students/Employees -- gated like every
            // other Dashboard widget (all-or-nothing; not in DefaultEnabledMenu), so a role must
            // be explicitly granted this before its users can search across records they might
            // not otherwise have list/detail permission to browse.
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_GLOBAL_SEARCH", "Global Search", "Dashboard", "GlobalSearch", 13));
            // Generic widget registry (2026-08-07) -- one endpoint, composes whichever
            // IsDashboardWidget menus (below) are granted to the caller's role. Not itself a
            // widget.
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_WIDGETS", "View Dashboard Widgets", "Dashboard", "GetWidgets", 14));

            // Logs (2026-07-28) -- System Access Logs and Error Logs were hidden PERMISSION rows
            // directly under DASHBOARD with no sidebar entry of their own; moved to their own main
            // menu with real (visible) SUB_MENU pages. Codes unchanged (ERROR_LOG_LIST/
            // ERROR_LOG_SUMMARY/ACCESS_LOG_LIST) so the sync pass just re-parents/re-types them in
            // place -- every existing role grant on these codes survives untouched.
            catalog.Add(MainMenu("LOGS", "Logs", "icons.FileSearchOutlined", 14, null));
            catalog.Add(SubMenu("LOGS", "ACCESS_LOG_LIST", "System Access Logs", "/logs/access", null, "Dashboard", "GetAccessLogs", 1));
            catalog.Add(SubMenu("LOGS", "ERROR_LOG_LIST", "Error Logs", "/logs/errors", null, "Dashboard", "GetErrorLogs", 2));
            catalog.Add(Permission("ERROR_LOG_LIST", "ERROR_LOG_SUMMARY", "View Error Summary", "Dashboard", "GetErrorSummary", 1));

            catalog.Add(MainMenu("USER_MANAGEMENT", "User Management", "icons.user", 2, null));
            catalog.Add(SubMenu("USER_MANAGEMENT", "USER_LIST", "Users", "/apps/account/list", "icons.user", "Users", "GetUsers", 1, isQuickLink: true));
            catalog.Add(Permission("USER_LIST", "USER_CREATE", "Create User", "Users", "CreateUser", 1));
            catalog.Add(Permission("USER_LIST", "USER_DETAIL", "View User Detail", "Users", "GetUserById", 2));
            catalog.Add(Permission("USER_LIST", "USER_UPDATE", "Update User", "Users", "UpdateUser", 3));
            catalog.Add(Permission("USER_LIST", "USER_DELETE", "Delete User", "Users", "DeleteUser", 4));
            // Per-user menu overrides (2026-08-07) -- additive grants direct to one user,
            // bypassing roles. Subject to the same privilege-escalation guard as ROLE_CLAIM_*.
            catalog.Add(Permission("USER_LIST", "USER_CLAIM_LIST", "View User Claims", "Users", "GetUserClaims", 5));
            catalog.Add(Permission("USER_LIST", "USER_CLAIM_ASSIGN", "Assign Menu To User", "Users", "AssignMenuToUser", 6));
            catalog.Add(Permission("USER_LIST", "USER_CLAIM_REMOVE", "Remove Menu From User", "Users", "RemoveMenuFromUser", 7));
            catalog.Add(SubMenu("USER_MANAGEMENT", "ROLE_LIST", "Roles & Permissions", "/apps/role/list", null, "Roles", "GetRoles", 2));
            catalog.Add(Permission("ROLE_LIST", "ROLE_CREATE", "Create Role", "Roles", "CreateRole", 1));
            catalog.Add(Permission("ROLE_LIST", "ROLE_DETAIL", "View Role Detail", "Roles", "GetRoleById", 2));
            catalog.Add(Permission("ROLE_LIST", "ROLE_UPDATE", "Update Role", "Roles", "UpdateRole", 3));
            catalog.Add(Permission("ROLE_LIST", "ROLE_DELETE", "Delete Role", "Roles", "DeleteRole", 4));
            catalog.Add(Permission("ROLE_LIST", "ROLE_USER_ROLES", "View User Roles", "Roles", "GetUserRoles", 5));
            catalog.Add(Permission("ROLE_LIST", "ROLE_CLAIM_ASSIGN", "Assign Menu To Role", "Roles", "AssignMenuToRole", 6));
            catalog.Add(Permission("ROLE_LIST", "ROLE_CLAIM_REMOVE", "Remove Menu From Role", "Roles", "RemoveMenuFromRole", 7));
            catalog.Add(Permission("ROLE_LIST", "ROLE_CLAIM_LIST", "View Role Claims", "Roles", "GetRoleClaims", 8));
            catalog.Add(Permission("ROLE_LIST", "ROLE_USER_ASSIGN", "Assign Role To User", "Roles", "AssignRoleToUser", 9));
            catalog.Add(Permission("ROLE_LIST", "ROLE_USER_REMOVE", "Remove Role From User", "Roles", "RemoveRoleFromUser", 10));

            catalog.Add(MainMenu("CONFIG_MANAGEMENT", "Master Settings", "icons.Settings", 4, null));
            catalog.Add(SubMenu("CONFIG_MANAGEMENT", "CONFIG_TYPE_LIST", "Config Types", "/apps/config-type/list", null, "Configs", "GetConfigTypes", 1, isQuickLink: true));
            catalog.Add(Permission("CONFIG_TYPE_LIST", "CONFIG_TYPE_CREATE", "Create Config Type", "Configs", "CreateConfigType", 1));
            catalog.Add(Permission("CONFIG_TYPE_LIST", "CONFIG_TYPE_DETAIL", "View Config Type Detail", "Configs", "GetConfigTypeById", 2));
            catalog.Add(Permission("CONFIG_TYPE_LIST", "CONFIG_TYPE_UPDATE", "Update Config Type", "Configs", "UpdateConfigType", 3));
            catalog.Add(Permission("CONFIG_TYPE_LIST", "CONFIG_TYPE_DELETE", "Delete Config Type", "Configs", "DeleteConfigType", 4));
            catalog.Add(Permission("CONFIG_TYPE_LIST", "CONFIG_CREATE", "Create Config", "Configs", "CreateConfig", 5));
            catalog.Add(Permission("CONFIG_TYPE_LIST", "CONFIG_DETAIL", "View Config Detail", "Configs", "GetConfigById", 6));
            catalog.Add(Permission("CONFIG_TYPE_LIST", "CONFIG_UPDATE", "Update Config", "Configs", "UpdateConfig", 7));
            catalog.Add(Permission("CONFIG_TYPE_LIST", "CONFIG_DELETE", "Delete Config", "Configs", "DeleteConfig", 8));
            catalog.Add(Permission("CONFIG_TYPE_LIST", "CONFIG_DROPDOWN", "View Config Dropdown", "Configs", "GetConfigsByTypeCode", 9));
            catalog.Add(SubMenu("CONFIG_MANAGEMENT", "APP_CONFIG_LIST", "App Configs", "/apps/appconfig/list", null, "AppConfigs", "GetAppConfigs", 2));
            catalog.Add(Permission("APP_CONFIG_LIST", "APP_CONFIG_CREATE", "Create App Config", "AppConfigs", "CreateAppConfig", 1));
            catalog.Add(Permission("APP_CONFIG_LIST", "APP_CONFIG_DETAIL", "View App Config Detail", "AppConfigs", "GetAppConfigById", 2));
            catalog.Add(Permission("APP_CONFIG_LIST", "APP_CONFIG_GROUP", "View App Configs By Group", "AppConfigs", "GetAppConfigsByGroup", 3));
            catalog.Add(Permission("APP_CONFIG_LIST", "APP_CONFIG_UPDATE", "Update App Config", "AppConfigs", "UpdateAppConfig", 4));
            catalog.Add(Permission("APP_CONFIG_LIST", "APP_CONFIG_DELETE", "Delete App Config", "AppConfigs", "DeleteAppConfig", 5));
            catalog.Add(SubMenu("CONFIG_MANAGEMENT", "MENU_LIST", "Menus", "/apps/menu/list", "icons.MenuOutlined", "Menus", "GetMenus", 3));
            catalog.Add(Permission("MENU_LIST", "MENU_CREATE", "Create Menu", "Menus", "CreateMenu", 1));
            catalog.Add(Permission("MENU_LIST", "MENU_DETAIL", "View Menu Detail", "Menus", "GetMenuById", 2));
            catalog.Add(Permission("MENU_LIST", "MENU_UPDATE", "Update Menu", "Menus", "UpdateMenu", 3));
            catalog.Add(Permission("MENU_LIST", "MENU_DELETE", "Delete Menu", "Menus", "DeleteMenu", 4));
            catalog.Add(SubMenu("CONFIG_MANAGEMENT", "DOCUMENT_TEMPLATE_LIST", "Document Templates", "/apps/document-template/list", null, "DocumentTemplates", "GetDocumentTemplates", 4));
            catalog.Add(Permission("DOCUMENT_TEMPLATE_LIST", "DOCUMENT_TEMPLATE_CREATE", "Create Document Template", "DocumentTemplates", "CreateDocumentTemplate", 1));
            catalog.Add(Permission("DOCUMENT_TEMPLATE_LIST", "DOCUMENT_TEMPLATE_DETAIL", "View Document Template Detail", "DocumentTemplates", "GetDocumentTemplateById", 2));
            catalog.Add(Permission("DOCUMENT_TEMPLATE_LIST", "DOCUMENT_TEMPLATE_UPDATE", "Update Document Template", "DocumentTemplates", "UpdateDocumentTemplate", 3));
            catalog.Add(Permission("DOCUMENT_TEMPLATE_LIST", "DOCUMENT_TEMPLATE_DELETE", "Delete Document Template", "DocumentTemplates", "DeleteDocumentTemplate", 4));
            catalog.Add(Permission("DOCUMENT_TEMPLATE_LIST", "DOCUMENT_TEMPLATE_PLACEHOLDERS", "View Document Template Placeholders", "DocumentTemplates", "GetPlaceholders", 5));

            // SETUP (2026-07-16): one lightweight home for all the configuration/master-data
            // submenus the operational modules consume -- academic structure (from the retired
            // ACADEMIC_MANAGEMENT main), fee configuration (from FEE_MANAGEMENT), and fiscal
            // years/tax slabs (from PAYROLL_MANAGEMENT). The moved submenus keep their codes, so
            // the sync pass re-parents the existing rows in place and every role-claim grant
            // survives. FEE_MANAGEMENT/PAYROLL_MANAGEMENT keep only transactional submenus.
            catalog.Add(MainMenu("SETUP", "Setup", "icons.ControlOutlined", 5, null));
            catalog.Add(SubMenu("SETUP", "YEAR_LIST", "Academic Years", "/apps/academic-year/list", null, "AcademicYears", "GetAcademicYears", 1, isQuickLink: true));
            catalog.Add(Permission("YEAR_LIST", "YEAR_CREATE", "Create Academic Year", "AcademicYears", "CreateAcademicYear", 1));
            catalog.Add(Permission("YEAR_LIST", "YEAR_DETAIL", "View Academic Year Detail", "AcademicYears", "GetAcademicYearById", 2));
            catalog.Add(Permission("YEAR_LIST", "YEAR_UPDATE", "Update Academic Year", "AcademicYears", "UpdateAcademicYear", 3));
            catalog.Add(Permission("YEAR_LIST", "YEAR_DELETE", "Delete Academic Year", "AcademicYears", "DeleteAcademicYear", 4));
            catalog.Add(Permission("YEAR_LIST", "YEAR_CLONE_STRUCTURE", "Clone Year Structure", "AcademicYears", "CloneStructure", 5));

            catalog.Add(Permission("SETUP", "FISCAL_YEAR_LIST", "Fiscal Years", "FiscalYears", "GetFiscalYears", 5));
            //catalog.Add(SubMenu("SETUP", "FISCAL_YEAR_LIST", "Fiscal Years", "/apps/fiscal-year/list", null, "FiscalYears", "GetFiscalYears", 5));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "FISCAL_YEAR_CREATE", "Create Fiscal Year", "FiscalYears", "CreateFiscalYear", 1));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "FISCAL_YEAR_DETAIL", "View Fiscal Year Detail", "FiscalYears", "GetFiscalYearById", 2));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "FISCAL_YEAR_UPDATE", "Update Fiscal Year", "FiscalYears", "UpdateFiscalYear", 3));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "FISCAL_YEAR_DELETE", "Delete Fiscal Year", "FiscalYears", "DeleteFiscalYear", 4));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "TAX_SLAB_ADD", "Add Tax Slab", "FiscalYears", "AddTaxSlab", 5));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "TAX_SLAB_LIST", "View Tax Slabs", "FiscalYears", "GetTaxSlabs", 6));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "TAX_SLAB_UPDATE", "Update Tax Slab", "FiscalYears", "UpdateTaxSlab", 7));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "TAX_SLAB_DELETE", "Delete Tax Slab", "FiscalYears", "RemoveTaxSlab", 8));


            catalog.Add(SubMenu("SETUP", "CLASS_LIST", "Classes", "/apps/academic-class/list", null, "AcademicClasses", "GetAcademicClasses", 2));
            catalog.Add(Permission("CLASS_LIST", "CLASS_CREATE", "Create Class", "AcademicClasses", "CreateAcademicClass", 1));
            catalog.Add(Permission("CLASS_LIST", "CLASS_DETAIL", "View Class Detail", "AcademicClasses", "GetAcademicClassById", 2));
            catalog.Add(Permission("CLASS_LIST", "CLASS_UPDATE", "Update Class", "AcademicClasses", "UpdateAcademicClass", 3));
            catalog.Add(Permission("CLASS_LIST", "CLASS_DELETE", "Delete Class", "AcademicClasses", "DeleteAcademicClass", 4));
            catalog.Add(Permission("CLASS_LIST", "CLASS_SUBJECT_ASSIGN", "Assign Subject To Class", "AcademicClasses", "AssignSubject", 5));
            catalog.Add(Permission("CLASS_LIST", "CLASS_SUBJECT_REMOVE", "Remove Subject From Class", "AcademicClasses", "RemoveSubject", 6));
            catalog.Add(Permission("CLASS_LIST", "CLASS_SUBJECT_LIST", "View Class Subjects", "AcademicClasses", "GetClassSubjects", 7));
            catalog.Add(Permission("CLASS_LIST", "CLASS_SECTION_ADD", "Add Section To Class", "AcademicClasses", "AddSection", 8));
            catalog.Add(Permission("CLASS_LIST", "CLASS_SECTION_LIST", "View Class Sections", "AcademicClasses", "GetSections", 9));
            catalog.Add(Permission("CLASS_LIST", "CLASS_SECTION_UPDATE", "Update Class Section", "AcademicClasses", "UpdateSection", 10));
            catalog.Add(Permission("CLASS_LIST", "CLASS_SECTION_REMOVE", "Remove Section From Class", "AcademicClasses", "RemoveSection", 11));
            // Class-scoped counterpart to TEACHER_ASSIGNMENT_BULK_ENTRY_ADD -- one class, several
            // teachers/subjects/sections/periods in one call, for mapping "who teaches this
            // class" from the class's own page.
            catalog.Add(Permission("CLASS_LIST", "CLASS_TEACHER_ASSIGNMENT_BULK_ENTRY_ADD", "Assign Teachers To Class (Bulk Entry)", "AcademicClasses", "AssignTeachersBulkEntry", 12));
            // Read-side counterpart -- "who teaches this class" (there was previously only the
            // bulk-create POST above, no GET).
            catalog.Add(Permission("CLASS_LIST", "CLASS_TEACHER_ASSIGNMENT_LIST", "View Class Teacher Assignments", "AcademicClasses", "GetTeacherAssignments", 13));

            // TEACHER_MANAGEMENT retired (2026-07-16): teachers were managed inside Employee
            // Management even then (the backend was already Employee-based via the shared-PK
            // split). The standalone Teacher entity/TeachersController/every TEACHER_* permission
            // row below it was retired entirely on 2026-08-06 (see BuildRetiredMenuCodes) -- a
            // teacher is now just an Employee categorized by EmployeeCategoryCode/JobPositionCode,
            // no separate profile, list, or permission tree. The genuinely non-duplicated pieces
            // (class/subject/section/period assignment, ID card preview) moved onto Employees
            // below as EMPLOYEE_ASSIGNMENT_*/EMPLOYEE_ID_CARD_PREVIEW; everything else (salary,
            // tax, payslip, loans) was always a thin alias into an EMPLOYEE_* permission that
            // already existed, so nothing new was needed for those.
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_ASSIGNMENT_ADD", "Assign Teacher", "Employees", "AssignClassSubject", 62));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_ASSIGNMENT_BULK_ADD", "Assign Teacher (Bulk Sections)", "Employees", "AssignClassSubjectBulk", 63));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_ASSIGNMENT_BULK_ENTRY_ADD", "Assign Teacher (Bulk Entry)", "Employees", "AssignClassSubjectBulkEntry", 64));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_ASSIGNMENT_REMOVE", "Remove Teacher Assignment", "Employees", "RemoveAssignment", 65));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_ASSIGNMENT_LIST", "View Teacher Assignments", "Employees", "GetAssignments", 66));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_ID_CARD_PREVIEW", "Preview Employee ID Card", "Employees", "GetIdCardPreview", 67));

            catalog.Add(MainMenu("STUDENT_MANAGEMENT", "Student Management", "icons.TeamOutlined", 8, null));
            catalog.Add(SubMenu("STUDENT_MANAGEMENT", "STUDENT_LIST", "Students", "/apps/student/list", null, "Students", "GetStudents", 1, isQuickLink: true));
            catalog.Add(Permission("STUDENT_LIST", "STUDENT_CREATE", "Create Student", "Students", "CreateStudent", 1));
            catalog.Add(Permission("STUDENT_LIST", "STUDENT_DETAIL", "View Student Detail", "Students", "GetStudentById", 2));
            catalog.Add(Permission("STUDENT_LIST", "STUDENT_UPDATE", "Update Student", "Students", "UpdateStudent", 3));
            catalog.Add(Permission("STUDENT_LIST", "STUDENT_DELETE", "Delete Student", "Students", "DeleteStudent", 4));
            catalog.Add(Permission("STUDENT_LIST", "STUDENT_GUARDIAN_LINK", "Link Guardian To Student", "Students", "LinkGuardian", 5));
            catalog.Add(Permission("STUDENT_LIST", "STUDENT_GUARDIAN_UNLINK", "Unlink Guardian From Student", "Students", "UnlinkGuardian", 6));
            catalog.Add(Permission("STUDENT_LIST", "STUDENT_GUARDIAN_LIST", "View Student Guardians", "Students", "GetGuardians", 7));
            catalog.Add(Permission("STUDENT_LIST", "STUDENT_DOCUMENT_UPLOAD", "Upload Student Document", "Students", "UploadDocument", 8));
            catalog.Add(Permission("STUDENT_LIST", "STUDENT_DOCUMENT_LIST", "View Student Documents", "Students", "GetDocuments", 9));
            catalog.Add(Permission("STUDENT_LIST", "STUDENT_DOCUMENT_DOWNLOAD", "Download Student Document", "Students", "DownloadDocument", 10));
            catalog.Add(Permission("STUDENT_LIST", "STUDENT_DOCUMENT_DELETE", "Delete Student Document", "Students", "DeleteDocument", 11));
            catalog.Add(Permission("STUDENT_LIST", "STUDENT_ID_CARD_PREVIEW", "Preview Student ID Card", "Students", "GetIdCardPreview", 12));
            // Portal account provisioning (2026-07-27) -- see the matching EMPLOYEE_REGISTER_ACCOUNT
            // comment under EMPLOYEE_LIST.
            catalog.Add(Permission("STUDENT_LIST", "STUDENT_REGISTER_ACCOUNT", "Register Student Portal Account", "Students", "RegisterUserAccount", 13));
            // "History"/"Current Class" tabs -- split out of the main student GET 2026-08-05 so
            // opening a profile doesn't pay for data most page loads never look at.
            catalog.Add(Permission("STUDENT_LIST", "STUDENT_ENROLLMENT_HISTORY", "View Student Enrollment History", "Students", "GetEnrollmentHistory", 14));
            catalog.Add(Permission("STUDENT_LIST", "STUDENT_TIMETABLE", "View Student Timetable", "Students", "GetTimetable", 15));
            catalog.Add(SubMenu("STUDENT_MANAGEMENT", "GUARDIAN_LIST", "Guardians", "/apps/guardian/list", null, "Guardians", "GetGuardians", 2));
            catalog.Add(Permission("GUARDIAN_LIST", "GUARDIAN_CREATE", "Create Guardian", "Guardians", "CreateGuardian", 1));
            catalog.Add(Permission("GUARDIAN_LIST", "GUARDIAN_DETAIL", "View Guardian Detail", "Guardians", "GetGuardianById", 2));
            catalog.Add(Permission("GUARDIAN_LIST", "GUARDIAN_UPDATE", "Update Guardian", "Guardians", "UpdateGuardian", 3));
            catalog.Add(Permission("GUARDIAN_LIST", "GUARDIAN_DELETE", "Delete Guardian", "Guardians", "DeleteGuardian", 4));
            catalog.Add(SubMenu("STUDENT_MANAGEMENT", "ENROLLMENT_LIST", "Enrollments", "/apps/enrollment/list", null, "Enrollments", "GetEnrollments", 3));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_CREATE", "Create Enrollment", "Enrollments", "CreateEnrollment", 1));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_DETAIL", "View Enrollment Detail", "Enrollments", "GetEnrollmentById", 2));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_UPDATE", "Update Enrollment", "Enrollments", "UpdateEnrollment", 3));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_DELETE", "Delete Enrollment", "Enrollments", "DeleteEnrollment", 4));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_SUBJECT_ADD", "Add Elective Subject", "Enrollments", "AddElectiveSubject", 5));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_SUBJECT_REMOVE", "Remove Elective Subject", "Enrollments", "RemoveElectiveSubject", 6));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_SUBJECT_LIST", "View Elective Subjects", "Enrollments", "GetElectiveSubjects", 7));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_DISCOUNT_ADD", "Add Discount", "Enrollments", "AddDiscount", 8));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_DISCOUNT_REMOVE", "Remove Discount", "Enrollments", "RemoveDiscount", 9));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_DISCOUNT_LIST", "View Discounts", "Enrollments", "GetDiscounts", 10));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_DISCOUNT_SUMMARY", "View Discount Summary", "Enrollments", "GetDiscountSummary", 11));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_SCHOLARSHIP_ADD", "Add Scholarship", "Enrollments", "AddScholarship", 12));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_SCHOLARSHIP_REMOVE", "Remove Scholarship", "Enrollments", "RemoveScholarship", 13));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_SCHOLARSHIP_LIST", "View Scholarships", "Enrollments", "GetScholarships", 14));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_SCHOLARSHIP_SUMMARY", "View Scholarship Summary", "Enrollments", "GetScholarshipSummary", 15));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_FEE_SELECTION_ADD", "Add Fee Selection", "Enrollments", "AddFeeSelection", 16));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_FEE_SELECTION_REMOVE", "Remove Fee Selection", "Enrollments", "RemoveFeeSelection", 17));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_FEE_SELECTION_LIST", "View Fee Selections", "Enrollments", "GetFeeSelections", 18));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_FEE_STRUCTURE_VIEW", "View Enrollment Fee Structure", "Enrollments", "GetFeeStructure", 19));
            catalog.Add(Permission("ENROLLMENT_LIST", "ENROLLMENT_FEE_RECEIPT_PREVIEW", "Preview Enrollment Fee Receipt", "Enrollments", "GetFeeReceiptPreview", 20));

            // Fee configuration lives under SETUP (moved 2026-07-16, code/Id unchanged);
            // FEE_MANAGEMENT below keeps only the transactional side. As of 2026-07-17 that's a
            // single sidebar item -- Fee Payments no longer has its own SUB_MENU, it's a tab on
            // the Fee Generation page (see the FEE_PAYMENT_* re-parenting note below).
            catalog.Add(SubMenu("SETUP", "FEE_STRUCTURE_LIST", "Fee Structures", "/apps/fee-structure/list", null, "FeeStructures", "GetFeeStructures", 3));
            catalog.Add(Permission("FEE_STRUCTURE_LIST", "FEE_STRUCTURE_CREATE", "Create Fee Structure", "FeeStructures", "CreateFeeStructure", 1));
            catalog.Add(Permission("FEE_STRUCTURE_LIST", "FEE_STRUCTURE_DETAIL", "View Fee Structure Detail", "FeeStructures", "GetFeeStructureById", 2));
            catalog.Add(Permission("FEE_STRUCTURE_LIST", "FEE_STRUCTURE_UPDATE", "Update Fee Structure", "FeeStructures", "UpdateFeeStructure", 3));
            catalog.Add(Permission("FEE_STRUCTURE_LIST", "FEE_STRUCTURE_DELETE", "Delete Fee Structure", "FeeStructures", "DeleteFeeStructure", 4));
            catalog.Add(Permission("FEE_STRUCTURE_LIST", "FEE_STRUCTURE_ITEM_ADD", "Add Fee Structure Item", "FeeStructures", "AddItem", 5));
            catalog.Add(Permission("FEE_STRUCTURE_LIST", "FEE_STRUCTURE_ITEM_UPDATE", "Update Fee Structure Item", "FeeStructures", "UpdateItem", 6));
            catalog.Add(Permission("FEE_STRUCTURE_LIST", "FEE_STRUCTURE_ITEM_REMOVE", "Remove Fee Structure Item", "FeeStructures", "RemoveItem", 7));

            catalog.Add(Permission("SETUP", "FEE_RULE_LIST", "Fee Rules", "FeeRules", "GetFeeRules", 1));
            catalog.Add(Permission("FEE_RULE_LIST", "FEE_RULE_CREATE", "Create Fee Rule", "FeeRules", "CreateFeeRule", 2));
            catalog.Add(Permission("FEE_RULE_LIST", "FEE_RULE_DETAIL", "View Fee Rule Detail", "FeeRules", "GetFeeRuleById", 3));
            catalog.Add(Permission("FEE_RULE_LIST", "FEE_RULE_UPDATE", "Update Fee Rule", "FeeRules", "UpdateFeeRule", 3));
            catalog.Add(Permission("FEE_RULE_LIST", "FEE_RULE_DELETE", "Delete Fee Rule", "FeeRules", "DeleteFeeRule", 4));

            // Fee Management and Payroll Management were merged into one generic ACCOUNTS main
            // menu (2026-08-03), since both only ever held one or two transactional submenus each
            // -- FEE_MANAGEMENT/PAYROLL_MANAGEMENT are retired below (BuildRetiredMenuCodes); the
            // submenus themselves (FEE_INVOICE_LIST, PAYROLL_RUN_LIST, SALARY_CALCULATOR) keep
            // their codes/ids, just re-parented, so existing role grants survive untouched.
            catalog.Add(MainMenu("ACCOUNTS", "Accounts", "icons.BankOutlined", 9, null));
            catalog.Add(SubMenu("ACCOUNTS", "FEE_INVOICE_LIST", "Fee Generation", "/apps/fee-invoice/list", null, "FeeInvoices", "GetFeeInvoices", 1, isQuickLink: true));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_INVOICE_GENERATE", "Generate Fee Invoices", "FeeInvoices", "Generate", 1));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_INVOICE_DETAIL", "View Fee Invoice Detail", "FeeInvoices", "GetFeeInvoiceById", 2));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_INVOICE_UPDATE", "Update Fee Invoice", "FeeInvoices", "UpdateFeeInvoice", 3));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_INVOICE_LINE_ADD", "Add Fee Invoice Line", "FeeInvoices", "AddLine", 4));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_INVOICE_LINE_UPDATE", "Update Fee Invoice Line", "FeeInvoices", "UpdateLine", 5));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_INVOICE_LINE_REMOVE", "Remove Fee Invoice Line", "FeeInvoices", "RemoveLine", 6));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_INVOICE_SETTLE_ANNUAL", "Settle Annual Fee In Full", "FeeInvoices", "SettleAnnualInFull", 24));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_INVOICE_FINALIZE", "Finalize Fee Invoices", "FeeInvoices", "Finalize", 7));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_INVOICE_CANCEL", "Cancel Fee Invoice", "FeeInvoices", "Cancel", 8));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_INVOICE_UNFINALIZE", "Unfinalize Fee Invoice", "FeeInvoices", "Unfinalize", 30));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_INVOICE_STATEMENT", "View Fee Statement", "FeeInvoices", "GetStatement", 9));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_ADJUSTMENT_LIST", "View Fee Adjustments", "FeeInvoices", "GetAdjustments", 10));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_ADJUSTMENT_CREATE", "Create Fee Adjustment", "FeeInvoices", "CreateAdjustment", 11));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_ADJUSTMENT_UPDATE", "Update Fee Adjustment", "FeeInvoices", "UpdateAdjustment", 12));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_ADJUSTMENT_CANCEL", "Cancel Fee Adjustment", "FeeInvoices", "CancelAdjustment", 13));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_ACCOUNT_STATEMENT", "View Statement of Account", "FeeInvoices", "GetAccountStatement", 14));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_STUDENT_SEARCH", "Search Students (Fee Module)", "FeeInvoices", "SearchStudents", 15));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_ADJUSTMENT_BULK_CREATE", "Bulk-Create Fee Adjustments", "FeeInvoices", "CreateBulkAdjustment", 16));

            // FEE_PAYMENT_LIST retired as a visible SUB_MENU (2026-07-17) -- Fee Payments folds
            // into a tab on the Fee Generation page instead of its own sidebar item. Same
            // TEACHER_LIST precedent as the 2026-07-16 ACADEMIC_MANAGEMENT/TEACHER_MANAGEMENT
            // retirement above: code and every child's code/id kept exactly, re-parented under
            // the surviving FEE_INVOICE_LIST sub-menu as hidden PERMISSION rows so existing role
            // grants survive untouched.
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_PAYMENT_LIST", "View Fee Payments (legacy list API)", "FeePayments", "GetPayments", 17));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_PAYMENT_PREVIEW", "Preview Fee Payment", "FeePayments", "Preview", 18));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_PAYMENT_CREATE", "Record Fee Payment", "FeePayments", "CreatePayment", 19));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_PAYMENT_DETAIL", "View Fee Payment Detail", "FeePayments", "GetPaymentById", 20));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_PAYMENT_VOID", "Void Fee Payment", "FeePayments", "VoidPayment", 21));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_PAYMENT_RECEIPT", "Print Fee Payment Receipt", "FeePayments", "GetReceipt", 22));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_PAYMENT_ADVANCE_QUOTE", "Quote Advance Fee Payment", "FeePayments", "GetAdvanceQuote", 23));

            // Fee generation's master table (2026-07-18): a period-keyed FeeGenerationRun header
            // grouping a billing month's invoices by class -> student, same "master table" role
            // PayrollRun plays on the payroll side. Hidden -- surfaced as a tab/drill-down on the
            // existing Fee Generation page, not its own sidebar item.
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_GENERATION_RUN_LIST", "View Fee Generation Runs", "FeeGenerationRuns", "GetFeeGenerationRuns", 25));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_GENERATION_RUN_DETAIL", "View Fee Generation Run Detail", "FeeGenerationRuns", "GetFeeGenerationRunById", 26));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_GENERATION_RUN_CLASS_DETAIL", "View Fee Generation Run Class Detail", "FeeGenerationRuns", "GetFeeGenerationRunClassDetail", 27));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_GENERATION_RUN_REFRESH", "Refresh Fee Generation Run", "FeeGenerationRuns", "RefreshRun", 28));
            catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_GENERATION_RUN_CLASS_REFRESH", "Refresh Fee Generation Run Class", "FeeGenerationRuns", "RefreshRunClass", 29));

            // Fiscal years/tax slabs live under SETUP (moved 2026-07-16, code/Id unchanged);
            // PAYROLL_MANAGEMENT below keeps only the transactional side (salary generation).

            catalog.Add(SubMenu("SETUP", "LEAVE_TYPE_LIST", "Leave Types", "/apps/leave-type/list", null, "LeaveTypes", "GetLeaveTypes", 1));
            catalog.Add(Permission("LEAVE_TYPE_LIST", "LEAVE_TYPE_CREATE", "Create Leave Type", "LeaveTypes", "CreateLeaveType", 1));
            catalog.Add(Permission("LEAVE_TYPE_LIST", "LEAVE_TYPE_DETAIL", "View Leave Type Detail", "LeaveTypes", "GetLeaveTypeById", 2));
            catalog.Add(Permission("LEAVE_TYPE_LIST", "LEAVE_TYPE_UPDATE", "Update Leave Type", "LeaveTypes", "UpdateLeaveType", 3));
            catalog.Add(Permission("LEAVE_TYPE_LIST", "LEAVE_TYPE_DELETE", "Delete Leave Type", "LeaveTypes", "DeleteLeaveType", 4));

            // Time Periods (2026-08-03) -- the school's daily routine catalog (periods + breaks)
            // and its per-class mapping, replacing the short-lived ConfigTypeCodes.ClassPeriod
            // catalog with real tables (Domain/Entities/TimePeriod + ClassTimePeriod) since
            // "certain classes run different period structures" is a relationship, not a flat
            // option list. See Docs/time_period_and_class_routine_implementation_guide.md.
            //catalog.Add(SubMenu("SETUP", "TIME_PERIOD_LIST", "Time Periods", "/apps/time-period/list", null, "TimePeriods", "GetTimePeriods", 7));
            catalog.Add(Permission("SETUP", "TIME_PERIOD_LIST", "Time Periods", "TimePeriods", "GetTimePeriods", 7));
            catalog.Add(Permission("TIME_PERIOD_LIST", "TIME_PERIOD_CREATE", "Create Time Period", "TimePeriods", "CreateTimePeriod", 1));
            catalog.Add(Permission("TIME_PERIOD_LIST", "TIME_PERIOD_DETAIL", "View Time Period Detail", "TimePeriods", "GetTimePeriodById", 2));
            catalog.Add(Permission("TIME_PERIOD_LIST", "TIME_PERIOD_UPDATE", "Update Time Period", "TimePeriods", "UpdateTimePeriod", 3));
            catalog.Add(Permission("TIME_PERIOD_LIST", "TIME_PERIOD_DELETE", "Delete Time Period", "TimePeriods", "DeleteTimePeriod", 4));
            catalog.Add(Permission("TIME_PERIOD_LIST", "TIME_PERIOD_MAP", "Map Time Periods To Classes (Bulk)", "TimePeriods", "MapClassTimePeriods", 5));
            catalog.Add(Permission("TIME_PERIOD_LIST", "TIME_PERIOD_CLASS_LIST", "View Class Time Periods", "TimePeriods", "GetClassTimePeriods", 6));
            catalog.Add(Permission("TIME_PERIOD_LIST", "TIME_PERIOD_UNMAP", "Unmap Time Period From Class", "TimePeriods", "UnmapClassTimePeriod", 7));


            catalog.Add(SubMenu("ACCOUNTS", "PAYROLL_RUN_LIST", "Salary Generation", "/apps/payroll-run/list", null, "PayrollRuns", "GetPayrollRuns", 2));
            catalog.Add(Permission("PAYROLL_RUN_LIST", "PAYROLL_RUN_CREATE", "Generate Payroll Run", "PayrollRuns", "CreatePayrollRun", 1));
            catalog.Add(Permission("PAYROLL_RUN_LIST", "PAYROLL_RUN_DETAIL", "View Payroll Run Detail", "PayrollRuns", "GetPayrollRunById", 2));
            catalog.Add(Permission("PAYROLL_RUN_LIST", "PAYROLL_RUN_APPROVE", "Approve Payroll Run", "PayrollRuns", "ApproveRun", 3));
            catalog.Add(Permission("PAYROLL_RUN_LIST", "PAYROLL_RUN_MARK_PAID", "Mark Payroll Run Paid", "PayrollRuns", "MarkPaid", 4));
            catalog.Add(Permission("PAYROLL_RUN_LIST", "PAYROLL_RUN_CANCEL", "Cancel Payroll Run", "PayrollRuns", "CancelRun", 5));
            catalog.Add(Permission("PAYROLL_RUN_LIST", "SALARY_SLIP_DETAIL", "View Salary Slip Detail", "PayrollRuns", "GetSlipById", 6));
            catalog.Add(Permission("PAYROLL_RUN_LIST", "SALARY_SLIP_CANCEL", "Cancel Salary Slip", "PayrollRuns", "CancelSlip", 7));
            catalog.Add(Permission("PAYROLL_RUN_LIST", "SALARY_SLIP_LINE_ADD", "Add Salary Slip Line", "PayrollRuns", "AddSlipLine", 8));
            catalog.Add(Permission("PAYROLL_RUN_LIST", "SALARY_SLIP_LINE_UPDATE", "Update Salary Slip Line", "PayrollRuns", "UpdateSlipLine", 9));
            catalog.Add(Permission("PAYROLL_RUN_LIST", "SALARY_SLIP_LINE_REMOVE", "Remove Salary Slip Line", "PayrollRuns", "RemoveSlipLine", 10));
            catalog.Add(Permission("PAYROLL_RUN_LIST", "PAYROLL_RUN_REFRESH", "Refresh Payroll Run", "PayrollRuns", "RefreshRun", 11));
            // 2026-07-22: individual (per-slip, not whole-run) approve + regenerate.
            catalog.Add(Permission("PAYROLL_RUN_LIST", "SALARY_SLIP_APPROVE", "Approve Salary Slip", "PayrollRuns", "ApproveSlip", 12));
            catalog.Add(Permission("PAYROLL_RUN_LIST", "SALARY_SLIP_REGENERATE", "Regenerate Salary Slip", "PayrollRuns", "RegenerateSlip", 13));
            catalog.Add(SubMenu("ACCOUNTS", "SALARY_CALCULATOR", "Salary Calculator", "/apps/payroll/salary-calculator", null, "SalaryCalculator", "CalculateSalaryStructure", 3));
            catalog.Add(Permission("SALARY_CALCULATOR", "SALARY_CALCULATOR_ASSIGN", "Assign Calculated Salary To Employee", "SalaryCalculator", "AssignSalaryStructure", 1));

            // Calendar configuration (BS month lengths, localization, weekly holidays) lives
            // under SETUP like the other master data; the calendar view + meetings get their
            // own CALENDAR_MANAGEMENT main below.
            catalog.Add(SubMenu("SETUP", "CALENDAR_CONFIG_LIST", "BS Calendar Setup", "/apps/calendar-config/list", null, "CalendarConfiguration", "GetBsMonthLengths", 6));
            catalog.Add(Permission("CALENDAR_CONFIG_LIST", "BS_MONTH_LENGTH_UPSERT", "Upsert BS Month Lengths", "CalendarConfiguration", "UpsertBsMonthLengths", 1));
            catalog.Add(Permission("CALENDAR_CONFIG_LIST", "CALENDAR_LOCALIZATION", "View Calendar Localization", "CalendarConfiguration", "GetLocalizationData", 2));
            catalog.Add(Permission("CALENDAR_CONFIG_LIST", "BS_WEEKDAY_UPDATE", "Update Weekday", "CalendarConfiguration", "UpdateWeekday", 3));

            catalog.Add(MainMenu("CALENDAR_MANAGEMENT", "Calendar", "icons.CalendarOutlined", 12, null));
            catalog.Add(SubMenu("CALENDAR_MANAGEMENT", "CALENDAR_VIEW", "Calendar", "/apps/calendar", null, "Calendar", "GetMonthView", 1));
            catalog.Add(Permission("CALENDAR_VIEW", "CALENDAR_TODAY", "View Today (Dual Date)", "Calendar", "GetToday", 1));
            catalog.Add(Permission("CALENDAR_VIEW", "CALENDAR_CONVERT_AD_BS", "Convert AD To BS", "Calendar", "ConvertAdToBs", 2));
            catalog.Add(Permission("CALENDAR_VIEW", "CALENDAR_CONVERT_BS_AD", "Convert BS To AD", "Calendar", "ConvertBsToAd", 3));
            catalog.Add(Permission("CALENDAR_VIEW", "CALENDAR_EVENT_LIST", "View Calendar Events", "Calendar", "GetCalendarEvents", 4));
            catalog.Add(Permission("CALENDAR_VIEW", "CALENDAR_EVENT_CREATE", "Create Calendar Event", "Calendar", "CreateCalendarEvent", 5));
            catalog.Add(Permission("CALENDAR_VIEW", "CALENDAR_EVENT_DETAIL", "View Calendar Event Detail", "Calendar", "GetCalendarEventById", 6));
            catalog.Add(Permission("CALENDAR_VIEW", "CALENDAR_EVENT_UPDATE", "Update Calendar Event", "Calendar", "UpdateCalendarEvent", 7));
            catalog.Add(Permission("CALENDAR_VIEW", "CALENDAR_EVENT_DELETE", "Delete Calendar Event", "Calendar", "DeleteCalendarEvent", 8));
            catalog.Add(Permission("CALENDAR_VIEW", "FESTIVAL_LIST", "View Festivals", "Calendar", "GetFestivals", 9));
            catalog.Add(Permission("CALENDAR_VIEW", "FESTIVAL_CREATE", "Create Festival", "Calendar", "CreateFestival", 10));
            catalog.Add(Permission("CALENDAR_VIEW", "FESTIVAL_DETAIL", "View Festival Detail", "Calendar", "GetFestivalById", 11));
            catalog.Add(Permission("CALENDAR_VIEW", "FESTIVAL_UPDATE", "Update Festival", "Calendar", "UpdateFestival", 12));
            catalog.Add(Permission("CALENDAR_VIEW", "FESTIVAL_DELETE", "Delete Festival", "Calendar", "DeleteFestival", 13));
            catalog.Add(SubMenu("CALENDAR_MANAGEMENT", "MEETING_LIST", "Meetings", "/apps/meeting/list", null, "Meetings", "GetMeetings", 2));
            catalog.Add(Permission("MEETING_LIST", "MEETING_SCHEDULE", "Schedule Meeting", "Meetings", "ScheduleMeeting", 1));
            catalog.Add(Permission("MEETING_LIST", "MEETING_DETAIL", "View Meeting Detail", "Meetings", "GetMeetingById", 2));
            catalog.Add(Permission("MEETING_LIST", "MEETING_UPDATE", "Update Meeting", "Meetings", "UpdateMeeting", 3));
            catalog.Add(Permission("MEETING_LIST", "MEETING_CANCEL", "Cancel Meeting", "Meetings", "DeleteMeeting", 4));
            catalog.Add(Permission("MEETING_LIST", "MEETING_RESPOND", "Respond To Invitation", "Meetings", "RespondToInvitation", 5));

            catalog.Add(MainMenu("EMPLOYEE_MANAGEMENT", "Employee Management", "icons.IdcardOutlined", 11, null));
            catalog.Add(SubMenu("EMPLOYEE_MANAGEMENT", "EMPLOYEE_LIST", "Employees", "/apps/employee/list", null, "Employees", "GetEmployees", 1, isQuickLink: true));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_CREATE", "Create Employee", "Employees", "CreateEmployee", 1));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_DETAIL", "View Employee Detail", "Employees", "GetEmployeeById", 2));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_UPDATE", "Update Employee", "Employees", "UpdateEmployee", 3));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_DELETE", "Delete Employee", "Employees", "DeleteEmployee", 4));
            // EMPLOYEE_TEACHER_PROMOTE retired (2026-08-06) -- teaching fields are now plain
            // optional Employee fields set via the normal Create/Update Employee call, no separate
            // "add teacher profile" step. See BuildRetiredMenuCodes.
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_ADD", "Add Employee Salary", "Employees", "AddSalary", 6));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_LIST", "View Employee Salary History", "Employees", "GetSalaryHistory", 7));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_COMPONENT_ADD", "Add Salary Component", "Employees", "AddSalaryComponent", 9));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_COMPONENT_REMOVE", "Remove Salary Component", "Employees", "RemoveSalaryComponent", 10));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_DEDUCTION_ADD", "Add Salary Deduction", "Employees", "AddSalaryDeduction", 11));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_DEDUCTION_REMOVE", "Remove Salary Deduction", "Employees", "RemoveSalaryDeduction", 12));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_INSURANCE_PREMIUM_ADD", "Add Insurance Premium", "Employees", "AddInsurancePremium", 13));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_INSURANCE_PREMIUM_REMOVE", "Remove Insurance Premium", "Employees", "RemoveInsurancePremium", 14));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_PAYSLIP_PREVIEW", "Preview Employee Payslip", "Employees", "GetPayslipPreview", 15));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_TAX_CALCULATION_MONTHLY", "View Employee Monthly Tax Breakdown", "Employees", "GetMonthlySalaryTaxCalculation", 16));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_PAYSLIP_LIST", "View Employee Payslip List", "Employees", "GetPayslips", 17));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_PAYSLIP_DETAIL", "View Employee Payslip Detail", "Employees", "GetPayslipDetail", 18));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LOAN_REQUEST", "Request Employee Loan", "Employees", "RequestLoan", 19));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LOAN_LIST", "View Employee Loans", "Employees", "GetLoans", 20));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LOAN_APPROVE", "Approve Employee Loan", "Employees", "ApproveLoan", 21));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LOAN_REJECT", "Reject Employee Loan", "Employees", "RejectLoan", 22));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LOAN_CANCEL", "Cancel Employee Loan", "Employees", "CancelLoan", 23));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_ADJUSTMENT_LIST", "View Salary Adjustments", "Employees", "GetSalaryAdjustments", 24));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_ADJUSTMENT_CREATE", "Create Salary Adjustment", "Employees", "CreateSalaryAdjustment", 25));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_ADJUSTMENT_UPDATE", "Update Salary Adjustment", "Employees", "UpdateSalaryAdjustment", 26));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_ADJUSTMENT_CANCEL", "Cancel Salary Adjustment", "Employees", "CancelSalaryAdjustment", 27));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_ADJUSTMENT_BULK", "Create Bulk Salary Adjustments", "Employees", "CreateBulkSalaryAdjustments", 28));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_FORECAST", "View Employee Salary Forecast", "Employees", "GetSalaryForecast", 29));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_TAX_PLANNING", "View Employee Tax Planning", "Employees", "GetTaxPlanning", 30));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_ANNUAL_FORECAST", "View Employee Annual Salary Forecast", "Employees", "GetAnnualForecast", 31));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_TAX_DETAILS_GRID", "View Employee Tax Details Grid", "Employees", "GetTaxDetailsGrid", 34));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_LINE_ADD", "Add Salary Line (Code-driven)", "Employees", "AddSalaryLine", 32));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_LINE_REMOVE", "Remove Salary Line (Code-driven)", "Employees", "RemoveSalaryLine", 33));

            // 2026-07-23: Qualifications and Documents, moved here from Teachers (retired below)
            // -- generic to every employee now, not teaching-specific.
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_QUALIFICATION_ADD", "Add Employee Qualification", "Employees", "AddQualification", 35));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_QUALIFICATION_REMOVE", "Remove Employee Qualification", "Employees", "RemoveQualification", 36));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_QUALIFICATION_LIST", "View Employee Qualifications", "Employees", "GetQualifications", 37));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_DOCUMENT_UPLOAD", "Upload Employee Document", "Employees", "UploadDocument", 38));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_DOCUMENT_LIST", "View Employee Documents", "Employees", "GetDocuments", 39));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_DOCUMENT_DOWNLOAD", "Download Employee Document", "Employees", "DownloadDocument", 40));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_DOCUMENT_DELETE", "Delete Employee Document", "Employees", "DeleteDocument", 41));

            // Document/qualification self-service + HR verification (2026-08-07) -- an employee
            // can upload their own document / add their own qualification (via the "Me" routes,
            // DefaultEnabledMenu-gated, no permission row) starting VerificationStatus.Pending;
            // these two Verify/Reject permission pairs are what an admin grants to whichever role
            // a school treats as "HR" to decide them. Not a hardcoded role -- same convention as
            // the Accounts/HR dashboard summaries.
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_QUALIFICATION_VERIFY", "Verify Employee Qualification", "Employees", "VerifyQualification", 69));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_QUALIFICATION_REJECT", "Reject Employee Qualification", "Employees", "RejectQualification", 70));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_DOCUMENT_VERIFY", "Verify Employee Document", "Employees", "VerifyDocument", 71));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_DOCUMENT_REJECT", "Reject Employee Document", "Employees", "RejectDocument", 72));

            // Leave Management (2026-07-23) -- photo, leave balances, leave requests, and
            // notifications are all Employee sub-resources (same "no dedicated cross-employee
            // list action" reasoning as Loans/Adjustments above), so they're permission rows
            // under EMPLOYEE_LIST rather than their own sub-menu. LeaveTypes (master data) gets
            // its own LEAVE_MANAGEMENT main menu below, since GetLeaveTypes is a real top-level
            // list action.
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_PHOTO_UPLOAD", "Upload Employee Photo", "Employees", "UploadPhoto", 42));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_PHOTO_DOWNLOAD", "Download Employee Photo", "Employees", "DownloadPhoto", 43));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_PHOTO_DELETE", "Delete Employee Photo", "Employees", "DeletePhoto", 44));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LEAVE_BALANCE_ALLOCATE", "Allocate Leave Balance", "Employees", "AllocateLeaveBalance", 45));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LEAVE_BALANCE_LIST", "View Leave Balances", "Employees", "GetLeaveBalances", 46));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LEAVE_REQUEST_CREATE", "Apply Leave", "Employees", "CreateLeaveRequest", 47));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LEAVE_REQUEST_LIST", "View Leave Requests", "Employees", "GetLeaveRequests", 48));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LEAVE_REQUEST_DETAIL", "View Leave Request Detail", "Employees", "GetLeaveRequestById", 49));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LEAVE_REQUEST_MANAGER_APPROVE", "Approve Leave (Manager)", "Employees", "ApproveManagerDecision", 50));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LEAVE_REQUEST_MANAGER_REJECT", "Reject Leave (Manager)", "Employees", "RejectManagerDecision", 51));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LEAVE_REQUEST_HR_APPROVE", "Approve Leave (HR)", "Employees", "ApproveHrDecision", 52));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LEAVE_REQUEST_HR_REJECT", "Reject Leave (HR)", "Employees", "RejectHrDecision", 53));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LEAVE_REQUEST_CANCEL", "Cancel Leave Request", "Employees", "CancelLeaveRequest", 54));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LEAVE_SUBSTITUTE_ADD", "Assign Leave Substitute", "Employees", "AddLeaveSubstitute", 55));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_LEAVE_SUBSTITUTE_REMOVE", "Remove Leave Substitute", "Employees", "RemoveLeaveSubstitute", 56));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_NOTIFICATION_LIST", "View Employee Notifications", "Employees", "GetNotifications", 57));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_NOTIFICATION_READ", "Mark Notification Read", "Employees", "MarkNotificationRead", 58));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_NOTIFICATION_READ_ALL", "Mark All Notifications Read", "Employees", "MarkAllNotificationsRead", 59));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_PROFILE_VIEW", "View Employee Profile", "Employees", "GetEmployeeProfile", 60));
            // Composite "Dashboard" page (2026-08-07) -- leave/routine/upcoming-events, the
            // admin-facing (id-scoped) counterpart of the self-service MY_DASHBOARD sub-menu below.
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_DASHBOARD_VIEW", "View Employee Dashboard", "Employees", "GetEmployeeDashboard", 68));

            // Portal account provisioning (2026-07-27) -- lets an admin create a login for an
            // existing Employee record that didn't get one at creation time. Same permission
            // gates the field on CreateEmployeeCommand implicitly, since that's just EMPLOYEE_CREATE.
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_REGISTER_ACCOUNT", "Register Employee Portal Account", "Employees", "RegisterUserAccount", 61));

            // Exam Management (2026-07-28) -- assessment configuration lives on ClassSubject
            // itself (extended in place, see AcademicClasses' CLASS_SUBJECT_ASSIGN/UPDATE
            // permissions above -- no new endpoint), so this main menu only covers the new
            // ExamTerm/Exam/ExamSchedule hierarchy. ExamSchedule create/update/delete
            // auto-manage a linked CalendarEvent (CalendarEventType.Exam) internally -- no
            // separate permission needed for that.
            // Round 2 (2026-07-28, same day): marks entry, result processing (GradeScale/
            // StudentExamMark/StudentResult), and student promotion (StudentPromotion) --
            // sections 3-5 of the design doc, completing the module started above.
            catalog.Add(MainMenu("EXAM_MANAGEMENT", "Exam Management", "icons.ScheduleOutlined", 15, null));
            catalog.Add(SubMenu("EXAM_MANAGEMENT", "EXAM_TERM_LIST", "Exam Terms", "/apps/exam-term/list", null, "ExamTerms", "GetExamTerms", 1));
            catalog.Add(Permission("EXAM_TERM_LIST", "EXAM_TERM_CREATE", "Create Exam Term", "ExamTerms", "CreateExamTerm", 1));
            catalog.Add(Permission("EXAM_TERM_LIST", "EXAM_TERM_DETAIL", "View Exam Term Detail", "ExamTerms", "GetExamTermById", 2));
            catalog.Add(Permission("EXAM_TERM_LIST", "EXAM_TERM_UPDATE", "Update Exam Term", "ExamTerms", "UpdateExamTerm", 3));
            catalog.Add(Permission("EXAM_TERM_LIST", "EXAM_TERM_DELETE", "Delete Exam Term", "ExamTerms", "DeleteExamTerm", 4));
            // Exam merges what used to be a separate Exam/ExamSchedule two-step flow into one
            // resource -- one subject's sitting for one section, date/time/room/invigilator
            // included, no separate "schedule" step. Full Marks/Pass Marks are never accepted
            // here; they come from the linked ClassSubject (CLASS_SUBJECT_ASSIGN/UPDATE above).
            catalog.Add(SubMenu("EXAM_MANAGEMENT", "EXAM_LIST", "Exams", "/apps/exam/list", null, "Exams", "GetExams", 2));
            catalog.Add(Permission("EXAM_LIST", "EXAM_CREATE", "Create Exam", "Exams", "CreateExam", 1));
            catalog.Add(Permission("EXAM_LIST", "EXAM_DETAIL", "View Exam Detail", "Exams", "GetExamById", 2));
            catalog.Add(Permission("EXAM_LIST", "EXAM_UPDATE", "Update Exam", "Exams", "UpdateExam", 3));
            catalog.Add(Permission("EXAM_LIST", "EXAM_DELETE", "Delete Exam", "Exams", "DeleteExam", 4));
            catalog.Add(Permission("EXAM_LIST", "EXAM_LOCK", "Lock Exam Marks", "Exams", "LockExam", 5));
            catalog.Add(Permission("EXAM_LIST", "EXAM_UNLOCK", "Unlock Exam Marks", "Exams", "UnlockExam", 6));
            // 2026-07-30, redesigned same day: schedule every subject of one class in one call --
            // the "set the whole routine at once" batch-scheduling workflow. Evolved from a
            // create-only/skip-list endpoint (EXAM_CREATE_ROUTINE, retired below) into a full
            // idempotent sync (create/update/remove) with term-boundary and overlap validation,
            // renamed to reflect that it's no longer just a create.
            catalog.Add(Permission("EXAM_LIST", "EXAM_SAVE_ROUTINE", "Save Exam Routine For Class", "Exams", "SaveExamRoutine", 7));
            catalog.Add(SubMenu("EXAM_MANAGEMENT", "GRADE_SCALE_LIST", "Grade Scales", "/apps/grade-scale/list", null, "GradeScales", "GetGradeScales", 4));
            catalog.Add(Permission("GRADE_SCALE_LIST", "GRADE_SCALE_CREATE", "Create Grade Scale", "GradeScales", "CreateGradeScale", 1));
            catalog.Add(Permission("GRADE_SCALE_LIST", "GRADE_SCALE_DETAIL", "View Grade Scale Detail", "GradeScales", "GetGradeScaleById", 2));
            catalog.Add(Permission("GRADE_SCALE_LIST", "GRADE_SCALE_UPDATE", "Update Grade Scale", "GradeScales", "UpdateGradeScale", 3));
            catalog.Add(Permission("GRADE_SCALE_LIST", "GRADE_SCALE_DELETE", "Delete Grade Scale", "GradeScales", "DeleteGradeScale", 4));
            catalog.Add(SubMenu("EXAM_MANAGEMENT", "STUDENT_EXAM_MARK_LIST", "Marks Entry", "/apps/exam-mark/list", null, "StudentExamMarks", "GetStudentExamMarks", 5));
            catalog.Add(Permission("STUDENT_EXAM_MARK_LIST", "STUDENT_EXAM_MARK_CREATE", "Create Student Exam Mark", "StudentExamMarks", "CreateStudentExamMark", 1));
            catalog.Add(Permission("STUDENT_EXAM_MARK_LIST", "STUDENT_EXAM_MARK_DETAIL", "View Student Exam Mark Detail", "StudentExamMarks", "GetStudentExamMarkById", 2));
            catalog.Add(Permission("STUDENT_EXAM_MARK_LIST", "STUDENT_EXAM_MARK_UPDATE", "Update Student Exam Mark", "StudentExamMarks", "UpdateStudentExamMark", 3));
            catalog.Add(Permission("STUDENT_EXAM_MARK_LIST", "STUDENT_EXAM_MARK_DELETE", "Delete Student Exam Mark", "StudentExamMarks", "DeleteStudentExamMark", 4));
            catalog.Add(Permission("STUDENT_EXAM_MARK_LIST", "STUDENT_EXAM_MARK_BULK_UPSERT", "Bulk Upsert Student Exam Marks", "StudentExamMarks", "BulkUpsertStudentExamMarks", 5));
            catalog.Add(Permission("STUDENT_EXAM_MARK_LIST", "STUDENT_EXAM_MARK_ROSTER", "View Student Exam Mark Roster", "StudentExamMarks", "GetStudentExamMarkRoster", 6));
            // 2026-07-30: admin, student-wise marks entry -- the "one student, every subject"
            // counterpart to the teacher-wise roster above.
            catalog.Add(Permission("STUDENT_EXAM_MARK_LIST", "STUDENT_EXAM_MARK_BY_STUDENT", "View Student Exam Marks By Student", "StudentExamMarks", "GetStudentExamMarksByStudent", 7));
            catalog.Add(SubMenu("EXAM_MANAGEMENT", "EXAM_RESULT_LIST", "Exam Results", "/apps/exam-result/list", null, "ExamResults", "GetStudentResults", 6));
            catalog.Add(Permission("EXAM_RESULT_LIST", "EXAM_RESULT_GENERATE", "Generate Exam Results", "ExamResults", "GenerateExamResults", 1));
            catalog.Add(Permission("EXAM_RESULT_LIST", "EXAM_RESULT_PUBLISH", "Publish Exam Results", "ExamResults", "PublishExamResults", 2));
            catalog.Add(Permission("EXAM_RESULT_LIST", "EXAM_RESULT_DETAIL", "View Exam Result Detail", "ExamResults", "GetStudentResultById", 3));
            catalog.Add(Permission("EXAM_RESULT_LIST", "EXAM_RESULT_WITHHOLD", "Withhold Exam Result", "ExamResults", "WithholdExamResult", 4));
            catalog.Add(Permission("EXAM_RESULT_LIST", "EXAM_RESULT_LIFT_WITHHOLD", "Lift Exam Result Withhold", "ExamResults", "LiftExamResultWithhold", 5));
            catalog.Add(SubMenu("EXAM_MANAGEMENT", "STUDENT_PROMOTION_LIST", "Student Promotions", "/apps/student-promotion/list", null, "StudentPromotions", "GetStudentPromotions", 7));
            catalog.Add(Permission("STUDENT_PROMOTION_LIST", "STUDENT_PROMOTION_CREATE", "Create Student Promotion", "StudentPromotions", "CreateStudentPromotion", 1));
            catalog.Add(Permission("STUDENT_PROMOTION_LIST", "STUDENT_PROMOTION_DETAIL", "View Student Promotion Detail", "StudentPromotions", "GetStudentPromotionById", 2));
            catalog.Add(Permission("STUDENT_PROMOTION_LIST", "STUDENT_PROMOTION_BULK_PROCESS", "Bulk Process Promotion", "StudentPromotions", "BulkProcessPromotion", 3));

            // Exam hall seat arrangement (ExamRoom/ExamHallArrangement/ExamSeatAllocation) was
            // removed entirely 2026-07-30, per instruction -- the module only needs simple
            // subject/date/time scheduling (see EXAM_CREATE_ROUTINE above). Their catalog rows
            // are retired below (BuildRetiredMenuCodes), not redefined here.

            // My Workspace (2026-08-06) -- self-service nav destinations for any Employee-linked
            // login (Teacher, Accountant, HR, Principal, ...), backed by EmployeesController's new
            // "me/..." actions. These rows exist purely for the nav tree / admin catalog --
            // AuthorizedAction never checks them for the "me" actions, since every one of those is
            // listed in appsettings.json's DefaultEnabledMenu and works for any authenticated
            // Employee-linked user regardless of grant. Grant these SUB_MENUs to whichever roles
            // should actually SEE the links (same one-time role-claims step as any other menu) --
            // access itself already works either way.
            catalog.Add(MainMenu("MY_WORKSPACE", "My Workspace", "icons.UserOutlined", 16, null));
            // Composite landing page (2026-08-07) -- leave summary/pending requests, class
            // routine + best-effort "next class", and upcoming holidays/events, one call
            // (GetMyDashboard). Order 1 so it's the workspace's default/first tab.
            catalog.Add(SubMenu("MY_WORKSPACE", "MY_DASHBOARD", "My Dashboard", "/apps/my-workspace/dashboard", null, "Employees", "GetMyDashboard", 1, isQuickLink: true, isDashboardWidget: true));
            catalog.Add(SubMenu("MY_WORKSPACE", "MY_PROFILE", "My Profile", "/apps/my-workspace/profile", null, "Employees", "GetMyProfile", 2));
            catalog.Add(SubMenu("MY_WORKSPACE", "MY_LEAVE", "Leave & Balance", "/apps/my-workspace/leave", null, "Employees", "GetMyLeaveBalances", 3, isQuickLink: true));
            catalog.Add(SubMenu("MY_WORKSPACE", "MY_PAYSLIPS", "Payslip & Taxes", "/apps/my-workspace/payslips", null, "Employees", "GetMyPayslips", 4));
            catalog.Add(SubMenu("MY_WORKSPACE", "MY_CLASSES", "My Classes", "/apps/my-workspace/classes", null, "Employees", "GetMyAssignments", 5));
            // Teacher data scoping (2026-08-07) -- "my students" landing page; marks entry is
            // reached in-context from a class/exam here, not a separate top-level nav item.
            catalog.Add(SubMenu("MY_WORKSPACE", "MY_STUDENTS", "My Students", "/apps/my-workspace/students", null, "Students", "GetMyStudents", 6));

            return catalog;
        }

        // Menu codes this seeder used to own that no longer exist in the catalog. The sync pass
        // only inserts and updates, so without this list a removed main menu would linger in the
        // database forever. Retired rows are soft-deleted (their Code stays reserved by the
        // unique index, same as any soft-deleted menu) -- ACADEMIC_MANAGEMENT and
        // TEACHER_MANAGEMENT were retired 2026-07-16 when their submenus moved to SETUP /
        // EMPLOYEE_LIST.
        private static List<string> BuildRetiredMenuCodes()
        {
            var retiredCodes = new List<string>
            {
                "ACADEMIC_MANAGEMENT",
                "TEACHER_MANAGEMENT",

                // Retired 2026-07-19: Statement of Account left the Student Management sidebar
                // -- it is now a tab on the student profile page, driven by the existing
                // GET /api/feeinvoices/account-statement/{enrollmentId} endpoint, whose
                // permission row (FEE_ACCOUNT_STATEMENT, under FEE_INVOICE_LIST) is unaffected.
                "STATEMENT_OF_ACCOUNT_LIST",

                // Retired 2026-07-23: GET /api/employees/{id}/salaries/tax-calculation removed
                // (superseded by GET .../salaries/tax-planning, which already composes on the
                // same underlying EmployeeService.GetCurrentSalaryTaxCalculationAsync -- that
                // method itself is unchanged). The /api/teachers/... alias that used to back this
                // was itself removed entirely on 2026-08-06, see the TEACHER_* block below.
                "EMPLOYEE_SALARY_TAX_CALCULATION",

                // Retired 2026-07-23: Qualifications and Documents moved off Teacher onto
                // Employee entirely (dbo.teacher_qualifications -> dbo.employee_qualifications,
                // dbo.teacher_documents -> dbo.employee_documents) -- neither concept was ever
                // teaching-specific. No Teacher-side alias was kept (unlike the tax-calculation
                // retirement above) -- these six codes are gone, replaced by
                // EMPLOYEE_QUALIFICATION_*/EMPLOYEE_DOCUMENT_* under Employees.
                "TEACHER_QUALIFICATION_ADD",
                "TEACHER_QUALIFICATION_REMOVE",
                "TEACHER_QUALIFICATION_LIST",
                "TEACHER_DOCUMENT_UPLOAD",
                "TEACHER_DOCUMENT_LIST",
                "TEACHER_DOCUMENT_DOWNLOAD",
                "TEACHER_DOCUMENT_DELETE",

                // Retired 2026-07-30: the exam hall seat-allocation subsystem (ExamRoom/
                // ExamHallArrangement/ExamHallArrangementClass/ExamSeatAllocation) was removed
                // entirely, per instruction -- the module only needs simple subject/date/time
                // scheduling. The "skip while live children exist" guard reads the DB, not this
                // pass's own in-memory flips, so the parent SubMenu rows below only finish
                // retiring on the boot after their children first get soft-deleted -- expected,
                // same self-healing-over-boots convention documented on the guard itself.
                "EXAM_ROOM_CREATE",
                "EXAM_ROOM_DETAIL",
                "EXAM_ROOM_UPDATE",
                "EXAM_ROOM_DELETE",
                "EXAM_ROOM_LIST",
                "EXAM_HALL_ARRANGEMENT_CREATE",
                "EXAM_HALL_ARRANGEMENT_DETAIL",
                "EXAM_HALL_ARRANGEMENT_UPDATE",
                "EXAM_HALL_ARRANGEMENT_DELETE",
                "EXAM_HALL_ARRANGEMENT_ADD_CLASS",
                "EXAM_HALL_ARRANGEMENT_REMOVE_CLASS",
                "EXAM_HALL_ARRANGEMENT_GENERATE_SEATING",
                "EXAM_HALL_ARRANGEMENT_LOCK",
                "EXAM_HALL_ARRANGEMENT_PUBLISH",
                "EXAM_HALL_ARRANGEMENT_MARK_ATTENDANCE",
                "EXAM_HALL_ARRANGEMENT_LIST",

                // Retired 2026-07-30, same day: EXAM_CREATE_ROUTINE (POST, create-only/skip-list)
                // replaced by EXAM_SAVE_ROUTINE (PUT, full idempotent sync) -- any role holding
                // the old grant needs EXAM_SAVE_ROUTINE granted instead.
                "EXAM_CREATE_ROUTINE",

                // Retired 2026-08-03: FEE_MANAGEMENT and PAYROLL_MANAGEMENT merged into one
                // generic ACCOUNTS main menu. FEE_INVOICE_LIST/PAYROLL_RUN_LIST/SALARY_CALCULATOR
                // kept their codes/ids, just re-parented under ACCOUNTS -- existing role grants
                // on those three survive untouched.
                "FEE_MANAGEMENT",
                "PAYROLL_MANAGEMENT",

                // Retired 2026-08-06: the standalone Teacher entity/TeachersController were
                // removed entirely -- a teacher is now just an Employee categorized by
                // EmployeeCategoryCode/JobPositionCode, per explicit instruction to simplify the
                // Role->Claims screen (26 near-duplicate TEACHER_* rows were inflating it). Full
                // CRUD/list (TEACHER_LIST/CREATE/DETAIL/UPDATE/DELETE) is gone with no replacement
                // -- Employee's own CRUD already covers it. Salary/tax/payslip/loan rows are gone
                // with no replacement -- each already had a real EMPLOYEE_* permission backing the
                // same operation (the Teacher-side ones were pure aliases). Assignment/ID-card rows
                // are replaced by EMPLOYEE_ASSIGNMENT_*/EMPLOYEE_ID_CARD_PREVIEW above (the one
                // genuinely non-duplicated slice of functionality). Any role holding any of these
                // grants loses them on next boot.
                "TEACHER_LIST",
                "TEACHER_CREATE",
                "TEACHER_DETAIL",
                "TEACHER_UPDATE",
                "TEACHER_DELETE",
                "TEACHER_ASSIGNMENT_ADD",
                "TEACHER_ASSIGNMENT_BULK_ADD",
                "TEACHER_ASSIGNMENT_BULK_ENTRY_ADD",
                "TEACHER_ASSIGNMENT_REMOVE",
                "TEACHER_ASSIGNMENT_LIST",
                "TEACHER_SALARY_ADD",
                "TEACHER_SALARY_LIST",
                "TEACHER_SALARY_TAX_CALCULATION",
                "TEACHER_PAYSLIP_PREVIEW",
                "TEACHER_ID_CARD_PREVIEW",
                "TEACHER_SALARY_TAX_CALCULATION_MONTHLY",
                "TEACHER_PAYSLIP_LIST",
                "TEACHER_PAYSLIP_DETAIL",
                "TEACHER_LOAN_REQUEST",
                "TEACHER_LOAN_LIST",
                "TEACHER_LOAN_APPROVE",
                "TEACHER_LOAN_REJECT",
                "TEACHER_LOAN_CANCEL",
                "TEACHER_SALARY_FORECAST",
                "TEACHER_TAX_PLANNING",
                "TEACHER_SALARY_ANNUAL_FORECAST",
                "TEACHER_TAX_DETAILS_GRID",
                "EMPLOYEE_TEACHER_PROMOTE"
            };

            return retiredCodes;
        }

        private static MenuSeedDefinition MainMenu(string code, string displayName, string icon, int order, string url)
        {
            var definition = new MenuSeedDefinition
            {
                Code = code,
                DisplayName = displayName,
                Url = url,
                Icon = icon,
                MenuType = MenuTypes.MainMenu,
                Order = order,
                IsHidden = false
            };

            return definition;
        }

        private static MenuSeedDefinition SubMenu(
            string parentCode,
            string code,
            string displayName,
            string url,
            string icon,
            string controller,
            string action,
            int order,
            bool isQuickLink = false,
            bool isDashboardWidget = false)
        {
            var definition = new MenuSeedDefinition
            {
                Code = code,
                DisplayName = displayName,
                Url = url,
                Icon = icon,
                MenuType = MenuTypes.SubMenu,
                Controller = controller,
                Action = action,
                ParentCode = parentCode,
                Order = order,
                IsHidden = false,
                IsQuickLink = isQuickLink,
                IsDashboardWidget = isDashboardWidget
            };

            return definition;
        }

        private static MenuSeedDefinition Permission(
            string parentCode,
            string code,
            string displayName,
            string controller,
            string action,
            int order,
            bool isDashboardWidget = false)
        {
            var definition = new MenuSeedDefinition
            {
                Code = code,
                DisplayName = displayName,
                MenuType = MenuTypes.Permission,
                Controller = controller,
                Action = action,
                ParentCode = parentCode,
                Order = order,
                IsHidden = true,
                IsDashboardWidget = isDashboardWidget
            };

            return definition;
        }

        private static async Task SeedMenusAsync(ApplicationDbContext dbContext)
        {
            var definitions = BuildMenuCatalog();

            // IgnoreQueryFilters: a soft-deleted row still owns its Code (unique index), so it
            // must be found and resurrected rather than blindly re-inserted.
            var existingMenus = await dbContext.Menus
                .IgnoreQueryFilters()
                .ToListAsync();

            var menusByCode = new Dictionary<string, Menu>();
            foreach (var menu in existingMenus)
            {
                menusByCode[menu.Code] = menu;
            }

            // Pass 1: create the missing rows first (parents unresolved) so that every code has
            // a database-assigned id before the hierarchy is wired up in pass 2.
            var anyMenuCreated = false;
            foreach (var definition in definitions)
            {
                if (menusByCode.ContainsKey(definition.Code))
                {
                    continue;
                }

                var menu = new Menu
                {
                    Code = definition.Code,
                    DisplayName = definition.DisplayName,
                    MenuType = definition.MenuType,
                    MenuFor = MenuAudience.Admin
                };

                dbContext.Menus.Add(menu);
                menusByCode[definition.Code] = menu;
                anyMenuCreated = true;
            }

            if (anyMenuCreated)
            {
                await dbContext.SaveChangesAsync();
            }

            // Pass 2: sync every catalog row to its definition. Unchanged values leave the row
            // untracked-as-modified, so a fully in-sync catalog is a no-op on startup.
            foreach (var definition in definitions)
            {
                var menu = menusByCode[definition.Code];

                int? parentId = null;
                if (definition.ParentCode != null)
                {
                    parentId = menusByCode[definition.ParentCode].Id;
                }

                menu.DisplayName = definition.DisplayName;
                menu.Url = definition.Url;
                menu.Icon = definition.Icon;
                menu.MenuType = definition.MenuType;
                menu.MenuFor = MenuAudience.Admin;
                menu.Controller = definition.Controller;
                menu.Action = definition.Action;
                menu.ParentId = parentId;
                menu.Order = definition.Order;
                menu.IsHidden = definition.IsHidden;
                menu.IsQuickLink = definition.IsQuickLink;
                menu.IsDashboardWidget = definition.IsDashboardWidget;
                menu.IsDeleted = false;
                menu.DeletedBy = null;
                menu.DeletedTs = null;
            }

            // Persist the sync BEFORE the retire pass: its has-children check queries the
            // database, so pass 2's re-parenting must already be visible there.
            await dbContext.SaveChangesAsync();

            // Pass 3 (retire): soft-delete catalog-owned rows whose code left the catalog. Runs
            // after the sync pass so any children have already been re-parented away; a retired
            // row that still has live children (a hand-created menu parented under it) is
            // skipped -- deleting it would orphan them in every tree build -- and picked up on a
            // later boot once the children move.
            var retiredCodes = BuildRetiredMenuCodes();
            foreach (var retiredCode in retiredCodes)
            {
                if (!menusByCode.TryGetValue(retiredCode, out var retiredMenu) || retiredMenu.IsDeleted)
                {
                    continue;
                }

                var hasLiveChildren = await dbContext.Menus
                    .AnyAsync(m => m.ParentId == retiredMenu.Id);
                if (hasLiveChildren)
                {
                    continue;
                }

                retiredMenu.IsDeleted = true;
                retiredMenu.DeletedBy = "system";
                retiredMenu.DeletedTs = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync();
        }

        private static async Task SeedSuperAdminRoleClaimsAsync(ApplicationDbContext dbContext, RoleManager<ApplicationRole> roleManager)
        {
            var superAdminRole = await roleManager.FindByNameAsync(RoleNames.SuperAdmin);
            if (superAdminRole == null)
            {
                return;
            }

            // Every menu carrying an endpoint needs a claim -- SUB_MENU list rows included,
            // since AuthorizedAction matches on Controller/Action regardless of MenuType.
            var endpointMenus = await dbContext.Menus
                .Where(m => m.Controller != null && m.Controller != "")
                .ToListAsync();

            var existingMenuIds = await dbContext.RoleClaims
                .Where(rc => rc.RoleId == superAdminRole.Id)
                .Select(rc => rc.MenuId)
                .ToListAsync();

            var newClaimsAdded = false;
            foreach (var endpointMenu in endpointMenus)
            {
                if (existingMenuIds.Contains(endpointMenu.Id))
                {
                    continue;
                }

                var roleClaim = new ApplicationRoleClaim
                {
                    RoleId = superAdminRole.Id,
                    MenuId = endpointMenu.Id,
                    ClaimType = "Permission",
                    ClaimValue = endpointMenu.Code
                };

                dbContext.RoleClaims.Add(roleClaim);
                newClaimsAdded = true;
            }

            if (newClaimsAdded)
            {
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
