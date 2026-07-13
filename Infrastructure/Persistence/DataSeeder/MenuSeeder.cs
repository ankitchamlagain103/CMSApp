using Domain.Constants;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.DataSeeder
{
    public static class MenuSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            await SeedMenusAsync(dbContext);
            await SeedSuperAdminRoleClaimsAsync(dbContext, roleManager);
        }

        private static async Task SeedMenusAsync(ApplicationDbContext dbContext)
        {
            var userManagement = await EnsureMainMenuAsync(dbContext, "USER_MANAGEMENT", "User Management", "users", 1);
            //var roleManagement = await EnsureMainMenuAsync(dbContext, "ROLE_MANAGEMENT", "Role Management", "shield", 2);
            //var menuManagement = await EnsureMainMenuAsync(dbContext, "MENU_MANAGEMENT", "Menu Management", "list", 3);
            var configManagement = await EnsureMainMenuAsync(dbContext, "CONFIG_MANAGEMENT", "Config Management", "settings", 4);
            //var dashboard = await EnsureMainMenuAsync(dbContext, "DASHBOARD", "Dashboard", "gauge", 5);

            //await EnsurePermissionAsync(dbContext, userManagement.Id, "USER_LIST", "View Users", "Users", "GetUsers", 1);
            //await EnsurePermissionAsync(dbContext, userManagement.Id, "USER_DETAIL", "View User Detail", "Users", "GetUserById", 2);
            //await EnsurePermissionAsync(dbContext, userManagement.Id, "USER_UPDATE", "Update User", "Users", "UpdateUser", 3);
            //await EnsurePermissionAsync(dbContext, userManagement.Id, "USER_DELETE", "Delete User", "Users", "DeleteUser", 4);
            await EnsurePermissionAsync(dbContext, userManagement.Id, "USER_CREATE", "Create User", "Users", "CreateUser", 5);

            //await EnsurePermissionAsync(dbContext, roleManagement.Id, "ROLE_LIST", "View Roles", "Roles", "GetRoles", 1);
            //await EnsurePermissionAsync(dbContext, roleManagement.Id, "ROLE_DETAIL", "View Role Detail", "Roles", "GetRoleById", 2);
            //await EnsurePermissionAsync(dbContext, roleManagement.Id, "ROLE_CREATE", "Create Role", "Roles", "CreateRole", 3);
            //await EnsurePermissionAsync(dbContext, roleManagement.Id, "ROLE_UPDATE", "Update Role", "Roles", "UpdateRole", 4);
            //await EnsurePermissionAsync(dbContext, roleManagement.Id, "ROLE_DELETE", "Delete Role", "Roles", "DeleteRole", 5);
            //await EnsurePermissionAsync(dbContext, roleManagement.Id, "ROLE_USER_ROLES", "View User Roles", "Roles", "GetUserRoles", 6);
            //await EnsurePermissionAsync(dbContext, roleManagement.Id, "ROLE_CLAIM_ASSIGN", "Assign Menu To Role", "Roles", "AssignMenuToRole", 7);
            //await EnsurePermissionAsync(dbContext, roleManagement.Id, "ROLE_CLAIM_REMOVE", "Remove Menu From Role", "Roles", "RemoveMenuFromRole", 8);
            //await EnsurePermissionAsync(dbContext, roleManagement.Id, "ROLE_CLAIM_LIST", "View Role Claims", "Roles", "GetRoleClaims", 9);
            //await EnsurePermissionAsync(dbContext, roleManagement.Id, "ROLE_USER_ASSIGN", "Assign Role To User", "Roles", "AssignRoleToUser", 10);
            //await EnsurePermissionAsync(dbContext, roleManagement.Id, "ROLE_USER_REMOVE", "Remove Role From User", "Roles", "RemoveRoleFromUser", 11);

            //await EnsurePermissionAsync(dbContext, menuManagement.Id, "MENU_LIST", "View Menus", "Menus", "GetMenus", 1);
            //await EnsurePermissionAsync(dbContext, menuManagement.Id, "MENU_DETAIL", "View Menu Detail", "Menus", "GetMenuById", 2);
            //await EnsurePermissionAsync(dbContext, menuManagement.Id, "MENU_CREATE", "Create Menu", "Menus", "CreateMenu", 3);
            //await EnsurePermissionAsync(dbContext, menuManagement.Id, "MENU_UPDATE", "Update Menu", "Menus", "UpdateMenu", 4);
            //await EnsurePermissionAsync(dbContext, menuManagement.Id, "MENU_DELETE", "Delete Menu", "Menus", "DeleteMenu", 5);

            //await EnsurePermissionAsync(dbContext, configManagement.Id, "CONFIG_TYPE_CREATE", "Create Config Type", "Configs", "CreateConfigType", 1);
            //await EnsurePermissionAsync(dbContext, configManagement.Id, "CONFIG_TYPE_LIST", "View Config Types", "Configs", "GetConfigTypes", 2);
            //await EnsurePermissionAsync(dbContext, configManagement.Id, "CONFIG_CREATE", "Create Config", "Configs", "CreateConfig", 3);
            //await EnsurePermissionAsync(dbContext, configManagement.Id, "CONFIG_DROPDOWN", "View Config Dropdown", "Configs", "GetConfigsByTypeCode", 4);
            //await EnsurePermissionAsync(dbContext, configManagement.Id, "CONFIG_UPDATE", "Update Config", "Configs", "UpdateConfig", 5);
            //await EnsurePermissionAsync(dbContext, configManagement.Id, "CONFIG_DELETE", "Delete Config", "Configs", "DeleteConfig", 6);
            //await EnsurePermissionAsync(dbContext, configManagement.Id, "CONFIG_TYPE_UPDATE", "Update Config Type", "Configs", "UpdateConfigType", 7);
            //await EnsurePermissionAsync(dbContext, configManagement.Id, "CONFIG_TYPE_DELETE", "Delete Config Type", "Configs", "DeleteConfigType", 8);
            //await EnsurePermissionAsync(dbContext, configManagement.Id, "CONFIG_TYPE_DETAIL", "View Config Type Detail", "Configs", "GetConfigTypeById", 9);
            //await EnsurePermissionAsync(dbContext, configManagement.Id, "CONFIG_DETAIL", "View Config Detail", "Configs", "GetConfigById", 10);

            await EnsurePermissionAsync(dbContext, configManagement.Id, "APP_CONFIG_CREATE", "Create App Config", "AppConfigs", "CreateAppConfig", 11);
            await EnsurePermissionAsync(dbContext, configManagement.Id, "APP_CONFIG_LIST", "View App Configs", "AppConfigs", "GetAppConfigs", 12);
            await EnsurePermissionAsync(dbContext, configManagement.Id, "APP_CONFIG_DETAIL", "View App Config Detail", "AppConfigs", "GetAppConfigById", 13);
            await EnsurePermissionAsync(dbContext, configManagement.Id, "APP_CONFIG_GROUP", "View App Configs By Group", "AppConfigs", "GetAppConfigsByGroup", 14);
            await EnsurePermissionAsync(dbContext, configManagement.Id, "APP_CONFIG_UPDATE", "Update App Config", "AppConfigs", "UpdateAppConfig", 15);
            await EnsurePermissionAsync(dbContext, configManagement.Id, "APP_CONFIG_DELETE", "Delete App Config", "AppConfigs", "DeleteAppConfig", 16);

            // Student management (2026-07-12): remember Controller must be the route value
            // (class name minus "Controller"), and every new gated endpoint needs a row here.
            var academicManagement = await EnsureMainMenuAsync(dbContext, "ACADEMIC_MANAGEMENT", "Academic Management", "calendar", 6);
            var teacherManagement = await EnsureMainMenuAsync(dbContext, "TEACHER_MANAGEMENT", "Teacher Management", "briefcase", 7);
            var studentManagement = await EnsureMainMenuAsync(dbContext, "STUDENT_MANAGEMENT", "Student Management", "graduation-cap", 8);

            await EnsurePermissionAsync(dbContext, academicManagement.Id, "YEAR_CREATE", "Create Academic Year", "AcademicYears", "CreateAcademicYear", 1);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "YEAR_LIST", "View Academic Years", "AcademicYears", "GetAcademicYears", 2);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "YEAR_DETAIL", "View Academic Year Detail", "AcademicYears", "GetAcademicYearById", 3);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "YEAR_UPDATE", "Update Academic Year", "AcademicYears", "UpdateAcademicYear", 4);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "YEAR_DELETE", "Delete Academic Year", "AcademicYears", "DeleteAcademicYear", 5);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "CLASS_CREATE", "Create Class", "AcademicClasses", "CreateAcademicClass", 6);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "CLASS_LIST", "View Classes", "AcademicClasses", "GetAcademicClasses", 7);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "CLASS_DETAIL", "View Class Detail", "AcademicClasses", "GetAcademicClassById", 8);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "CLASS_UPDATE", "Update Class", "AcademicClasses", "UpdateAcademicClass", 9);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "CLASS_DELETE", "Delete Class", "AcademicClasses", "DeleteAcademicClass", 10);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "CLASS_SUBJECT_ASSIGN", "Assign Subject To Class", "AcademicClasses", "AssignSubject", 11);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "CLASS_SUBJECT_REMOVE", "Remove Subject From Class", "AcademicClasses", "RemoveSubject", 12);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "CLASS_SUBJECT_LIST", "View Class Subjects", "AcademicClasses", "GetClassSubjects", 13);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "CLASS_SECTION_ADD", "Add Section To Class", "AcademicClasses", "AddSection", 14);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "CLASS_SECTION_LIST", "View Class Sections", "AcademicClasses", "GetSections", 15);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "CLASS_SECTION_UPDATE", "Update Class Section", "AcademicClasses", "UpdateSection", 16);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "CLASS_SECTION_REMOVE", "Remove Section From Class", "AcademicClasses", "RemoveSection", 17);
            await EnsurePermissionAsync(dbContext, academicManagement.Id, "YEAR_CLONE_STRUCTURE", "Clone Year Structure", "AcademicYears", "CloneStructure", 18);

            await EnsurePermissionAsync(dbContext, teacherManagement.Id, "TEACHER_CREATE", "Create Teacher", "Teachers", "CreateTeacher", 1);
            await EnsurePermissionAsync(dbContext, teacherManagement.Id, "TEACHER_LIST", "View Teachers", "Teachers", "GetTeachers", 2);
            await EnsurePermissionAsync(dbContext, teacherManagement.Id, "TEACHER_DETAIL", "View Teacher Detail", "Teachers", "GetTeacherById", 3);
            await EnsurePermissionAsync(dbContext, teacherManagement.Id, "TEACHER_UPDATE", "Update Teacher", "Teachers", "UpdateTeacher", 4);
            await EnsurePermissionAsync(dbContext, teacherManagement.Id, "TEACHER_DELETE", "Delete Teacher", "Teachers", "DeleteTeacher", 5);
            await EnsurePermissionAsync(dbContext, teacherManagement.Id, "TEACHER_QUALIFICATION_ADD", "Add Teacher Qualification", "Teachers", "AddQualification", 6);
            await EnsurePermissionAsync(dbContext, teacherManagement.Id, "TEACHER_QUALIFICATION_REMOVE", "Remove Teacher Qualification", "Teachers", "RemoveQualification", 7);
            await EnsurePermissionAsync(dbContext, teacherManagement.Id, "TEACHER_QUALIFICATION_LIST", "View Teacher Qualifications", "Teachers", "GetQualifications", 8);
            await EnsurePermissionAsync(dbContext, teacherManagement.Id, "TEACHER_ASSIGNMENT_ADD", "Assign Teacher", "Teachers", "AssignClassSubject", 9);
            await EnsurePermissionAsync(dbContext, teacherManagement.Id, "TEACHER_ASSIGNMENT_REMOVE", "Remove Teacher Assignment", "Teachers", "RemoveAssignment", 10);
            await EnsurePermissionAsync(dbContext, teacherManagement.Id, "TEACHER_ASSIGNMENT_LIST", "View Teacher Assignments", "Teachers", "GetAssignments", 11);
            await EnsurePermissionAsync(dbContext, teacherManagement.Id, "TEACHER_DOCUMENT_UPLOAD", "Upload Teacher Document", "Teachers", "UploadDocument", 12);
            await EnsurePermissionAsync(dbContext, teacherManagement.Id, "TEACHER_DOCUMENT_LIST", "View Teacher Documents", "Teachers", "GetDocuments", 13);
            await EnsurePermissionAsync(dbContext, teacherManagement.Id, "TEACHER_DOCUMENT_DOWNLOAD", "Download Teacher Document", "Teachers", "DownloadDocument", 14);
            await EnsurePermissionAsync(dbContext, teacherManagement.Id, "TEACHER_DOCUMENT_DELETE", "Delete Teacher Document", "Teachers", "DeleteDocument", 15);

            await EnsurePermissionAsync(dbContext, studentManagement.Id, "STUDENT_CREATE", "Create Student", "Students", "CreateStudent", 1);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "STUDENT_LIST", "View Students", "Students", "GetStudents", 2);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "STUDENT_DETAIL", "View Student Detail", "Students", "GetStudentById", 3);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "STUDENT_UPDATE", "Update Student", "Students", "UpdateStudent", 4);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "STUDENT_DELETE", "Delete Student", "Students", "DeleteStudent", 5);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "STUDENT_GUARDIAN_LINK", "Link Guardian To Student", "Students", "LinkGuardian", 6);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "STUDENT_GUARDIAN_UNLINK", "Unlink Guardian From Student", "Students", "UnlinkGuardian", 7);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "STUDENT_GUARDIAN_LIST", "View Student Guardians", "Students", "GetGuardians", 8);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "GUARDIAN_CREATE", "Create Guardian", "Guardians", "CreateGuardian", 9);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "GUARDIAN_LIST", "View Guardians", "Guardians", "GetGuardians", 10);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "GUARDIAN_DETAIL", "View Guardian Detail", "Guardians", "GetGuardianById", 11);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "GUARDIAN_UPDATE", "Update Guardian", "Guardians", "UpdateGuardian", 12);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "GUARDIAN_DELETE", "Delete Guardian", "Guardians", "DeleteGuardian", 13);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "ENROLLMENT_CREATE", "Create Enrollment", "Enrollments", "CreateEnrollment", 14);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "ENROLLMENT_LIST", "View Enrollments", "Enrollments", "GetEnrollments", 15);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "ENROLLMENT_DETAIL", "View Enrollment Detail", "Enrollments", "GetEnrollmentById", 16);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "ENROLLMENT_UPDATE", "Update Enrollment", "Enrollments", "UpdateEnrollment", 17);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "ENROLLMENT_DELETE", "Delete Enrollment", "Enrollments", "DeleteEnrollment", 18);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "ENROLLMENT_SUBJECT_ADD", "Add Elective Subject", "Enrollments", "AddElectiveSubject", 19);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "ENROLLMENT_SUBJECT_REMOVE", "Remove Elective Subject", "Enrollments", "RemoveElectiveSubject", 20);
            await EnsurePermissionAsync(dbContext, studentManagement.Id, "ENROLLMENT_SUBJECT_LIST", "View Elective Subjects", "Enrollments", "GetElectiveSubjects", 21);

            //await EnsurePermissionAsync(dbContext, dashboard.Id, "ERROR_LOG_LIST", "View Error Logs", "Dashboard", "GetErrorLogs", 1);
            //await EnsurePermissionAsync(dbContext, dashboard.Id, "ERROR_LOG_SUMMARY", "View Error Summary", "Dashboard", "GetErrorSummary", 2);
            //await EnsurePermissionAsync(dbContext, dashboard.Id, "ACCESS_LOG_LIST", "View Access Logs", "Dashboard", "GetAccessLogs", 3);
            //await EnsurePermissionAsync(dbContext, dashboard.Id, "DASHBOARD_SUMMARY", "View Dashboard Summary", "Dashboard", "GetSummary", 4);
        }

        private static async Task<Menu> EnsureMainMenuAsync(
            ApplicationDbContext dbContext,
            string code,
            string displayName,
            string icon,
            int order)
        {
            var existingMenu = await dbContext.Menus.FirstOrDefaultAsync(m => m.Code == code);
            if (existingMenu != null)
            {
                return existingMenu;
            }

            var menu = new Menu
            {
                Code = code,
                DisplayName = displayName,
                Icon = icon,
                MenuType = MenuTypes.MainMenu,
                MenuFor = MenuAudience.Admin,
                Order = order,
                IsHidden = false
            };

            dbContext.Menus.Add(menu);
            await dbContext.SaveChangesAsync();
            return menu;
        }

        private static async Task EnsurePermissionAsync(
            ApplicationDbContext dbContext,
            int parentId,
            string code,
            string displayName,
            string controller,
            string action,
            int order)
        {
            var permissionExists = await dbContext.Menus.AnyAsync(m => m.Code == code);
            if (permissionExists)
            {
                return;
            }

            var permission = new Menu
            {
                Code = code,
                DisplayName = displayName,
                MenuType = MenuTypes.Permission,
                MenuFor = MenuAudience.Admin,
                Controller = controller,
                Action = action,
                ParentId = parentId,
                Order = order,
                IsHidden = true
            };

            dbContext.Menus.Add(permission);
            await dbContext.SaveChangesAsync();
        }

        private static async Task SeedSuperAdminRoleClaimsAsync(ApplicationDbContext dbContext, RoleManager<ApplicationRole> roleManager)
        {
            var superAdminRole = await roleManager.FindByNameAsync(RoleNames.SuperAdmin);
            if (superAdminRole == null)
            {
                return;
            }

            var permissionMenus = await dbContext.Menus
                .Where(m => m.MenuType == MenuTypes.Permission)
                .ToListAsync();

            var existingMenuIds = await dbContext.RoleClaims
                .Where(rc => rc.RoleId == superAdminRole.Id)
                .Select(rc => rc.MenuId)
                .ToListAsync();

            var newClaimsAdded = false;
            foreach (var permissionMenu in permissionMenus)
            {
                if (existingMenuIds.Contains(permissionMenu.Id))
                {
                    continue;
                }

                var roleClaim = new ApplicationRoleClaim
                {
                    RoleId = superAdminRole.Id,
                    MenuId = permissionMenu.Id,
                    ClaimType = "Permission",
                    ClaimValue = permissionMenu.Code
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
