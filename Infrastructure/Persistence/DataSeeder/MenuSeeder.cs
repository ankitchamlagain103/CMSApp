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

            catalog.Add(MainMenu("ACADEMIC_MANAGEMENT", "Academic Management", "icons.ReadOutlined", 6, null));
            catalog.Add(SubMenu("ACADEMIC_MANAGEMENT", "YEAR_LIST", "Academic Years", "/apps/academic-year/list", null, "AcademicYears", "GetAcademicYears", 1));
            catalog.Add(Permission("YEAR_LIST", "YEAR_CREATE", "Create Academic Year", "AcademicYears", "CreateAcademicYear", 1));
            catalog.Add(Permission("YEAR_LIST", "YEAR_DETAIL", "View Academic Year Detail", "AcademicYears", "GetAcademicYearById", 2));
            catalog.Add(Permission("YEAR_LIST", "YEAR_UPDATE", "Update Academic Year", "AcademicYears", "UpdateAcademicYear", 3));
            catalog.Add(Permission("YEAR_LIST", "YEAR_DELETE", "Delete Academic Year", "AcademicYears", "DeleteAcademicYear", 4));
            catalog.Add(Permission("YEAR_LIST", "YEAR_CLONE_STRUCTURE", "Clone Year Structure", "AcademicYears", "CloneStructure", 5));
            catalog.Add(SubMenu("ACADEMIC_MANAGEMENT", "CLASS_LIST", "Classes", "/apps/academic-class/list", null, "AcademicClasses", "GetAcademicClasses", 2));
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

            catalog.Add(MainMenu("TEACHER_MANAGEMENT", "Teacher Management", "icons.SolutionOutlined", 7, null));
            catalog.Add(SubMenu("TEACHER_MANAGEMENT", "TEACHER_LIST", "Teachers", "/apps/teacher/list", null, "Teachers", "GetTeachers", 1));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_CREATE", "Create Teacher", "Teachers", "CreateTeacher", 1));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_DETAIL", "View Teacher Detail", "Teachers", "GetTeacherById", 2));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_UPDATE", "Update Teacher", "Teachers", "UpdateTeacher", 3));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_DELETE", "Delete Teacher", "Teachers", "DeleteTeacher", 4));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_QUALIFICATION_ADD", "Add Teacher Qualification", "Teachers", "AddQualification", 5));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_QUALIFICATION_REMOVE", "Remove Teacher Qualification", "Teachers", "RemoveQualification", 6));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_QUALIFICATION_LIST", "View Teacher Qualifications", "Teachers", "GetQualifications", 7));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_ASSIGNMENT_ADD", "Assign Teacher", "Teachers", "AssignClassSubject", 8));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_ASSIGNMENT_REMOVE", "Remove Teacher Assignment", "Teachers", "RemoveAssignment", 9));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_ASSIGNMENT_LIST", "View Teacher Assignments", "Teachers", "GetAssignments", 10));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_DOCUMENT_UPLOAD", "Upload Teacher Document", "Teachers", "UploadDocument", 11));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_DOCUMENT_LIST", "View Teacher Documents", "Teachers", "GetDocuments", 12));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_DOCUMENT_DOWNLOAD", "Download Teacher Document", "Teachers", "DownloadDocument", 13));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_DOCUMENT_DELETE", "Delete Teacher Document", "Teachers", "DeleteDocument", 14));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_SALARY_ADD", "Add Teacher Salary", "Teachers", "AddSalary", 15));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_SALARY_LIST", "View Teacher Salary History", "Teachers", "GetSalaryHistory", 16));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_SALARY_TAX_CALCULATION", "View Teacher Tax Calculation", "Teachers", "GetSalaryTaxCalculation", 17));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_PAYSLIP_PREVIEW", "Preview Teacher Payslip", "Teachers", "GetPayslipPreview", 18));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_ID_CARD_PREVIEW", "Preview Teacher ID Card", "Teachers", "GetIdCardPreview", 19));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_SALARY_TAX_CALCULATION_MONTHLY", "View Teacher Monthly Tax Breakdown", "Teachers", "GetMonthlySalaryTaxCalculation", 20));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_PAYSLIP_LIST", "View Teacher Payslip List", "Teachers", "GetPayslips", 21));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_PAYSLIP_DETAIL", "View Teacher Payslip Detail", "Teachers", "GetPayslipDetail", 22));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_LOAN_REQUEST", "Request Teacher Loan", "Teachers", "RequestLoan", 23));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_LOAN_LIST", "View Teacher Loans", "Teachers", "GetLoans", 24));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_LOAN_APPROVE", "Approve Teacher Loan", "Teachers", "ApproveLoan", 25));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_LOAN_REJECT", "Reject Teacher Loan", "Teachers", "RejectLoan", 26));
            catalog.Add(Permission("TEACHER_LIST", "TEACHER_LOAN_CANCEL", "Cancel Teacher Loan", "Teachers", "CancelLoan", 27));

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

            catalog.Add(MainMenu("FEE_MANAGEMENT", "Fee Management", "icons.DollarOutlined", 9, null));
            catalog.Add(SubMenu("FEE_MANAGEMENT", "FEE_STRUCTURE_LIST", "Fee Structures", "/apps/fee-structure/list", null, "FeeStructures", "GetFeeStructures", 1));
            catalog.Add(Permission("FEE_STRUCTURE_LIST", "FEE_STRUCTURE_CREATE", "Create Fee Structure", "FeeStructures", "CreateFeeStructure", 1));
            catalog.Add(Permission("FEE_STRUCTURE_LIST", "FEE_STRUCTURE_DETAIL", "View Fee Structure Detail", "FeeStructures", "GetFeeStructureById", 2));
            catalog.Add(Permission("FEE_STRUCTURE_LIST", "FEE_STRUCTURE_UPDATE", "Update Fee Structure", "FeeStructures", "UpdateFeeStructure", 3));
            catalog.Add(Permission("FEE_STRUCTURE_LIST", "FEE_STRUCTURE_DELETE", "Delete Fee Structure", "FeeStructures", "DeleteFeeStructure", 4));
            catalog.Add(Permission("FEE_STRUCTURE_LIST", "FEE_STRUCTURE_ITEM_ADD", "Add Fee Structure Item", "FeeStructures", "AddItem", 5));
            catalog.Add(Permission("FEE_STRUCTURE_LIST", "FEE_STRUCTURE_ITEM_UPDATE", "Update Fee Structure Item", "FeeStructures", "UpdateItem", 6));
            catalog.Add(Permission("FEE_STRUCTURE_LIST", "FEE_STRUCTURE_ITEM_REMOVE", "Remove Fee Structure Item", "FeeStructures", "RemoveItem", 7));

            catalog.Add(MainMenu("PAYROLL_MANAGEMENT", "Payroll Management", "icons.BankOutlined", 10, null));
            catalog.Add(SubMenu("PAYROLL_MANAGEMENT", "FISCAL_YEAR_LIST", "Fiscal Years", "/apps/fiscal-year/list", null, "FiscalYears", "GetFiscalYears", 1));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "FISCAL_YEAR_CREATE", "Create Fiscal Year", "FiscalYears", "CreateFiscalYear", 1));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "FISCAL_YEAR_DETAIL", "View Fiscal Year Detail", "FiscalYears", "GetFiscalYearById", 2));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "FISCAL_YEAR_UPDATE", "Update Fiscal Year", "FiscalYears", "UpdateFiscalYear", 3));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "FISCAL_YEAR_DELETE", "Delete Fiscal Year", "FiscalYears", "DeleteFiscalYear", 4));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "TAX_SLAB_ADD", "Add Tax Slab", "FiscalYears", "AddTaxSlab", 5));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "TAX_SLAB_LIST", "View Tax Slabs", "FiscalYears", "GetTaxSlabs", 6));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "TAX_SLAB_UPDATE", "Update Tax Slab", "FiscalYears", "UpdateTaxSlab", 7));
            catalog.Add(Permission("FISCAL_YEAR_LIST", "TAX_SLAB_DELETE", "Delete Tax Slab", "FiscalYears", "RemoveTaxSlab", 8));

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

            return catalog;
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
