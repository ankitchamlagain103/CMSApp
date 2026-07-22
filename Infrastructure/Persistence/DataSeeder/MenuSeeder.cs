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
            catalog.Add(Permission("DASHBOARD", "ERROR_LOG_LIST", "View Error Logs", "Dashboard", "GetErrorLogs", 1));
            catalog.Add(Permission("DASHBOARD", "ERROR_LOG_SUMMARY", "View Error Summary", "Dashboard", "GetErrorSummary", 2));
            catalog.Add(Permission("DASHBOARD", "ACCESS_LOG_LIST", "View Access Logs", "Dashboard", "GetAccessLogs", 3));
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_SUMMARY", "View Dashboard Summary", "Dashboard", "GetSummary", 4));
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_ENROLLMENT_STATS", "View Enrollment Stats", "Dashboard", "GetEnrollmentStats", 5));
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_TEACHER_WIDGET", "View Teacher List Widget", "Dashboard", "GetTeacherListWidget", 6));
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_USER_WIDGET", "View User List Widget", "Dashboard", "GetUserListWidget", 7));
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_BAR_GRAPH", "View Dashboard Bar Graph", "Dashboard", "GetBarGraph", 8));
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_CURRENT_ACADEMIC_YEAR", "View Current Academic Year", "Dashboard", "GetCurrentAcademicYear", 9));
            catalog.Add(Permission("DASHBOARD", "DASHBOARD_QUICK_MENUS", "View Quick Menu Suggestions", "Dashboard", "GetQuickMenus", 10));

            catalog.Add(MainMenu("USER_MANAGEMENT", "User Management", "icons.user", 2, null));
            catalog.Add(SubMenu("USER_MANAGEMENT", "USER_LIST", "Users", "/apps/account/list", "icons.user", "Users", "GetUsers", 1));
            catalog.Add(Permission("USER_LIST", "USER_CREATE", "Create User", "Users", "CreateUser", 1));
            catalog.Add(Permission("USER_LIST", "USER_DETAIL", "View User Detail", "Users", "GetUserById", 2));
            catalog.Add(Permission("USER_LIST", "USER_UPDATE", "Update User", "Users", "UpdateUser", 3));
            catalog.Add(Permission("USER_LIST", "USER_DELETE", "Delete User", "Users", "DeleteUser", 4));
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
            catalog.Add(SubMenu("CONFIG_MANAGEMENT", "CONFIG_TYPE_LIST", "Config Types", "/apps/config-type/list", null, "Configs", "GetConfigTypes", 1));
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
            catalog.Add(SubMenu("SETUP", "YEAR_LIST", "Academic Years", "/apps/academic-year/list", null, "AcademicYears", "GetAcademicYears", 1));
            catalog.Add(Permission("YEAR_LIST", "YEAR_CREATE", "Create Academic Year", "AcademicYears", "CreateAcademicYear", 1));
            catalog.Add(Permission("YEAR_LIST", "YEAR_DETAIL", "View Academic Year Detail", "AcademicYears", "GetAcademicYearById", 2));
            catalog.Add(Permission("YEAR_LIST", "YEAR_UPDATE", "Update Academic Year", "AcademicYears", "UpdateAcademicYear", 3));
            catalog.Add(Permission("YEAR_LIST", "YEAR_DELETE", "Delete Academic Year", "AcademicYears", "DeleteAcademicYear", 4));
            catalog.Add(Permission("YEAR_LIST", "YEAR_CLONE_STRUCTURE", "Clone Year Structure", "AcademicYears", "CloneStructure", 5));
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

            // TEACHER_MANAGEMENT retired (2026-07-16): teachers are managed inside Employee
            // Management (the backend was already Employee-based via the shared-PK split; this
            // removes the separate nav module). The /api/teachers/* alias endpoints stay, so
            // every TEACHER_* permission row survives -- re-parented under EMPLOYEE_LIST as
            // hidden PERMISSION rows (codes and Ids unchanged, grants preserved). TEACHER_LIST
            // itself was that module's SUB_MENU; it doubles as the Teachers/GetTeachers
            // permission, so it is redefined as a hidden permission rather than retired.
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_LIST", "View Teachers (legacy list API)", "Teachers", "GetTeachers", 30));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_CREATE", "Create Teacher", "Teachers", "CreateTeacher", 31));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_DETAIL", "View Teacher Detail", "Teachers", "GetTeacherById", 32));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_UPDATE", "Update Teacher", "Teachers", "UpdateTeacher", 33));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_DELETE", "Delete Teacher", "Teachers", "DeleteTeacher", 34));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_QUALIFICATION_ADD", "Add Teacher Qualification", "Teachers", "AddQualification", 35));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_QUALIFICATION_REMOVE", "Remove Teacher Qualification", "Teachers", "RemoveQualification", 36));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_QUALIFICATION_LIST", "View Teacher Qualifications", "Teachers", "GetQualifications", 37));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_ASSIGNMENT_ADD", "Assign Teacher", "Teachers", "AssignClassSubject", 38));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_ASSIGNMENT_REMOVE", "Remove Teacher Assignment", "Teachers", "RemoveAssignment", 39));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_ASSIGNMENT_LIST", "View Teacher Assignments", "Teachers", "GetAssignments", 40));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_DOCUMENT_UPLOAD", "Upload Teacher Document", "Teachers", "UploadDocument", 41));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_DOCUMENT_LIST", "View Teacher Documents", "Teachers", "GetDocuments", 42));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_DOCUMENT_DOWNLOAD", "Download Teacher Document", "Teachers", "DownloadDocument", 43));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_DOCUMENT_DELETE", "Delete Teacher Document", "Teachers", "DeleteDocument", 44));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_SALARY_ADD", "Add Teacher Salary", "Teachers", "AddSalary", 45));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_SALARY_LIST", "View Teacher Salary History", "Teachers", "GetSalaryHistory", 46));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_SALARY_TAX_CALCULATION", "View Teacher Tax Calculation", "Teachers", "GetSalaryTaxCalculation", 47));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_PAYSLIP_PREVIEW", "Preview Teacher Payslip", "Teachers", "GetPayslipPreview", 48));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_ID_CARD_PREVIEW", "Preview Teacher ID Card", "Teachers", "GetIdCardPreview", 49));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_SALARY_TAX_CALCULATION_MONTHLY", "View Teacher Monthly Tax Breakdown", "Teachers", "GetMonthlySalaryTaxCalculation", 50));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_PAYSLIP_LIST", "View Teacher Payslip List", "Teachers", "GetPayslips", 51));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_PAYSLIP_DETAIL", "View Teacher Payslip Detail", "Teachers", "GetPayslipDetail", 52));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_LOAN_REQUEST", "Request Teacher Loan", "Teachers", "RequestLoan", 53));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_LOAN_LIST", "View Teacher Loans", "Teachers", "GetLoans", 54));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_LOAN_APPROVE", "Approve Teacher Loan", "Teachers", "ApproveLoan", 55));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_LOAN_REJECT", "Reject Teacher Loan", "Teachers", "RejectLoan", 56));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_LOAN_CANCEL", "Cancel Teacher Loan", "Teachers", "CancelLoan", 57));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_SALARY_FORECAST", "View Teacher Salary Forecast", "Teachers", "GetSalaryForecast", 58));
            catalog.Add(Permission("EMPLOYEE_LIST", "TEACHER_TAX_PLANNING", "View Teacher Tax Planning", "Teachers", "GetTaxPlanning", 59));

            catalog.Add(MainMenu("STUDENT_MANAGEMENT", "Student Management", "icons.TeamOutlined", 8, null));
            catalog.Add(SubMenu("STUDENT_MANAGEMENT", "STUDENT_LIST", "Students", "/apps/student/list", null, "Students", "GetStudents", 1));
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

            catalog.Add(SubMenu("SETUP", "FEE_RULE_LIST", "Fee Rules", "/apps/fee-rule/list", null, "FeeRules", "GetFeeRules", 4));
            catalog.Add(Permission("FEE_RULE_LIST", "FEE_RULE_CREATE", "Create Fee Rule", "FeeRules", "CreateFeeRule", 1));
            catalog.Add(Permission("FEE_RULE_LIST", "FEE_RULE_DETAIL", "View Fee Rule Detail", "FeeRules", "GetFeeRuleById", 2));
            catalog.Add(Permission("FEE_RULE_LIST", "FEE_RULE_UPDATE", "Update Fee Rule", "FeeRules", "UpdateFeeRule", 3));
            catalog.Add(Permission("FEE_RULE_LIST", "FEE_RULE_DELETE", "Delete Fee Rule", "FeeRules", "DeleteFeeRule", 4));

            catalog.Add(MainMenu("FEE_MANAGEMENT", "Fee Management", "icons.DollarOutlined", 9, null));
            catalog.Add(SubMenu("FEE_MANAGEMENT", "FEE_INVOICE_LIST", "Fee Generation", "/apps/fee-invoice/list", null, "FeeInvoices", "GetFeeInvoices", 1));
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
            catalog.Add(SubMenu("SETUP", "FISCAL_YEAR_LIST", "Fiscal Years", "/apps/fiscal-year/list", null, "FiscalYears", "GetFiscalYears", 5));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "FISCAL_YEAR_CREATE", "Create Fiscal Year", "FiscalYears", "CreateFiscalYear", 1));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "FISCAL_YEAR_DETAIL", "View Fiscal Year Detail", "FiscalYears", "GetFiscalYearById", 2));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "FISCAL_YEAR_UPDATE", "Update Fiscal Year", "FiscalYears", "UpdateFiscalYear", 3));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "FISCAL_YEAR_DELETE", "Delete Fiscal Year", "FiscalYears", "DeleteFiscalYear", 4));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "TAX_SLAB_ADD", "Add Tax Slab", "FiscalYears", "AddTaxSlab", 5));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "TAX_SLAB_LIST", "View Tax Slabs", "FiscalYears", "GetTaxSlabs", 6));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "TAX_SLAB_UPDATE", "Update Tax Slab", "FiscalYears", "UpdateTaxSlab", 7));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "TAX_SLAB_DELETE", "Delete Tax Slab", "FiscalYears", "RemoveTaxSlab", 8));

            catalog.Add(MainMenu("PAYROLL_MANAGEMENT", "Payroll Management", "icons.BankOutlined", 10, null));
            catalog.Add(SubMenu("PAYROLL_MANAGEMENT", "PAYROLL_RUN_LIST", "Salary Generation", "/apps/payroll-run/list", null, "PayrollRuns", "GetPayrollRuns", 1));
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
            catalog.Add(SubMenu("PAYROLL_MANAGEMENT", "SALARY_CALCULATOR", "Salary Calculator", "/apps/payroll/salary-calculator", null, "SalaryCalculator", "CalculateSalaryStructure", 2));
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
            catalog.Add(SubMenu("EMPLOYEE_MANAGEMENT", "EMPLOYEE_LIST", "Employees", "/apps/employee/list", null, "Employees", "GetEmployees", 1));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_CREATE", "Create Employee", "Employees", "CreateEmployee", 1));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_DETAIL", "View Employee Detail", "Employees", "GetEmployeeById", 2));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_UPDATE", "Update Employee", "Employees", "UpdateEmployee", 3));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_DELETE", "Delete Employee", "Employees", "DeleteEmployee", 4));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_TEACHER_PROMOTE", "Add Teacher Profile", "Employees", "PromoteToTeacher", 5));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_ADD", "Add Employee Salary", "Employees", "AddSalary", 6));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_LIST", "View Employee Salary History", "Employees", "GetSalaryHistory", 7));
            catalog.Add(Permission("EMPLOYEE_LIST", "EMPLOYEE_SALARY_TAX_CALCULATION", "View Employee Tax Calculation", "Employees", "GetSalaryTaxCalculation", 8));
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
                "STATEMENT_OF_ACCOUNT_LIST"
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
            int order)
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
                IsHidden = false
            };

            return definition;
        }

        private static MenuSeedDefinition Permission(
            string parentCode,
            string code,
            string displayName,
            string controller,
            string action,
            int order)
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
                IsHidden = true
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
