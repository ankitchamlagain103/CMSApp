using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.DataSeeder
{
    // Development/demo sample data for the student-management sub-system:
    //
    //   1. Grade (Nursery..Twelve), Section (A/B/C) and Subject dropdown options in the
    //      Config catalog (extends what ConfigCatalogSeeder deliberately leaves to admins).
    //   2. One class per grade for the current academic year, sections A/B per class, and
    //      the class-subject mapping following the common Nepali school curriculum
    //      (mandatory class-wide rows, plus optional/elective rows for grades 9-12).
    //   3. 20 teachers with qualifications, per-subject assignments (round-robin) and one
    //      class-teacher per section A.
    //   4. 100 students (50 families -> father + mother guardians, ~2 children each),
    //      enrolled across all grades with per-section roll numbers, and elective picks
    //      for grades 9-12.
    //
    // Idempotent, create-if-missing by each record's natural key (grade code, employee no,
    // admission no, guardian email, ...) -- it never updates or deletes existing rows, so
    // admin edits survive restarts. All uniqueness checks use IgnoreQueryFilters where the
    // key stays reserved by a soft-deleted row. This is sample data for development: remove
    // the SampleDataSeeder call from Program.cs for a real deployment.
    public static class SampleDataSeeder
    {
        private const int SectionCapacity = 30;
        private const int TeacherCount = 20;
        private const int StudentCount = 100;

        private static readonly string[] GradeCodes =
        {
            "NURSERY", "LKG", "UKG",
            "ONE", "TWO", "THREE", "FOUR", "FIVE",
            "SIX", "SEVEN", "EIGHT", "NINE", "TEN",
            "ELEVEN", "TWELVE"
        };

        private static readonly string[] GradeLabels =
        {
            "Nursery", "LKG", "UKG",
            "One", "Two", "Three", "Four", "Five",
            "Six", "Seven", "Eight", "Nine", "Ten",
            "Eleven", "Twelve"
        };

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await SeedCatalogOptionsAsync(dbContext);

            var academicYear = await EnsureCurrentAcademicYearAsync(dbContext);
            if (academicYear == null)
            {
                return;
            }

            var classesByGrade = await SeedClassesAsync(dbContext, academicYear);
            var sectionsByClassId = await SeedSectionsAsync(dbContext, classesByGrade);
            var classSubjects = await SeedClassSubjectsAsync(dbContext, classesByGrade);
            var teachers = await SeedTeachersAsync(dbContext);
            await SeedTeacherAssignmentsAsync(dbContext, teachers, classesByGrade, sectionsByClassId, classSubjects);
            await SeedStudentsAsync(dbContext, academicYear, classesByGrade, sectionsByClassId, classSubjects);
        }

        private static async Task SeedCatalogOptionsAsync(ApplicationDbContext dbContext)
        {
            var catalogTypeCodes = new[] { ConfigTypeCodes.Grade, ConfigTypeCodes.Section, ConfigTypeCodes.Subject };
            var existingConfigs = await dbContext.Configs
                .Where(c => catalogTypeCodes.Contains(c.TypeCode))
                .ToListAsync();

            var existingOptionKeys = new HashSet<string>();
            foreach (var config in existingConfigs)
            {
                existingOptionKeys.Add(config.TypeCode + "|" + config.Code);
            }

            for (var gradeIndex = 0; gradeIndex < GradeCodes.Length; gradeIndex++)
            {
                AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Grade, GradeCodes[gradeIndex], GradeLabels[gradeIndex], gradeIndex + 1);
            }

            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Section, "A", "Section A", 1);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Section, "B", "Section B", 2);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Section, "C", "Section C", 3);

            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "NEPALI", "Nepali", 1);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "ENGLISH", "English", 2);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "MATH", "Mathematics", 3);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "SCIENCE", "Science", 4);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "OPT_MATHS", "Optional Mathematics", 5);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "OPT_ACCOUNT", "Optional Account", 6);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "RHYMES", "Rhymes", 7);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "DRAWING", "Drawing & Coloring", 8);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "GENERAL_AWARENESS", "General Awareness", 9);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "PLAY_ACTIVITIES", "Play Activities", 10);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "MUSIC", "Music", 11);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "PHYSICAL_ACTIVITIES", "Physical Activities", 12);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "ENV_AWARENESS", "Environmental Awareness", 13);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "GAMES", "Games", 14);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "EVS", "Environmental Studies (EVS)", 15);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "GK", "General Knowledge", 16);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "PHYSICAL_EDUCATION", "Physical Education", 17);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "OUR_SURROUNDINGS", "Our Surroundings", 18);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "CREATIVE_ARTS", "Creative Arts", 19);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "HPE", "Health & Physical Education", 20);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "SCIENCE_TECH", "Science & Technology", 21);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "SOCIAL_STUDIES", "Social Studies", 22);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "SOCIAL_HV", "Social Studies & Human Values", 23);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "HPCA", "Health, Physical & Creative Arts", 24);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "LOCAL_SUBJECT", "Local Subject", 25);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "COMPUTER_SCIENCE", "Computer Science", 26);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "SOCIAL_LIFE_SKILLS", "Social Studies & Life Skills", 27);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "PHYSICS", "Physics", 28);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "CHEMISTRY", "Chemistry", 29);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "BIOLOGY", "Biology", 30);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "ACCOUNTANCY", "Accountancy", 31);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "ECONOMICS", "Economics", 32);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "BUSINESS_STUDIES", "Business Studies", 33);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "SOCIOLOGY", "Sociology", 34);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "PSYCHOLOGY", "Psychology", 35);
            AddOptionIfMissing(dbContext, existingOptionKeys, ConfigTypeCodes.Subject, "HOTEL_MANAGEMENT", "Hotel Management", 36);

            await dbContext.SaveChangesAsync();
        }

        private static void AddOptionIfMissing(
            ApplicationDbContext dbContext,
            HashSet<string> existingOptionKeys,
            int typeCode,
            string code,
            string label,
            int order)
        {
            var optionKey = typeCode + "|" + code;
            if (existingOptionKeys.Contains(optionKey))
            {
                return;
            }

            var config = new Config
            {
                TypeCode = typeCode,
                Code = code,
                Label = label,
                Order = order
            };

            dbContext.Configs.Add(config);
            existingOptionKeys.Add(optionKey);
        }

        private static async Task<AcademicYear> EnsureCurrentAcademicYearAsync(ApplicationDbContext dbContext)
        {
            var currentYear = await dbContext.AcademicYears.FirstOrDefaultAsync(y => y.IsCurrent);
            if (currentYear != null)
            {
                return currentYear;
            }

            var latestYear = await dbContext.AcademicYears
                .OrderByDescending(y => y.StartDate)
                .FirstOrDefaultAsync();
            if (latestYear != null)
            {
                return latestYear;
            }

            // A soft-deleted year still reserves its Code -- nothing sane to attach to then.
            var codeReserved = await dbContext.AcademicYears
                .IgnoreQueryFilters()
                .AnyAsync(y => y.Code == "AY2083");
            if (codeReserved)
            {
                return null;
            }

            var academicYear = new AcademicYear
            {
                Code = "AY2083",
                Name = "Academic Year 2083/84",
                StartDate = new DateTime(2026, 4, 14),
                EndDate = new DateTime(2027, 4, 13),
                IsCurrent = true,
                Status = RecordStatus.Active
            };

            dbContext.AcademicYears.Add(academicYear);
            await dbContext.SaveChangesAsync();
            return academicYear;
        }

        private static async Task<Dictionary<string, AcademicClass>> SeedClassesAsync(
            ApplicationDbContext dbContext,
            AcademicYear academicYear)
        {
            var existingClasses = await dbContext.AcademicClasses
                .IgnoreQueryFilters()
                .Where(c => c.AcademicYearId == academicYear.Id)
                .ToListAsync();

            var existingByGrade = new Dictionary<string, AcademicClass>();
            foreach (var academicClass in existingClasses)
            {
                existingByGrade[academicClass.GradeCode] = academicClass;
            }

            var classesByGrade = new Dictionary<string, AcademicClass>();
            foreach (var gradeCode in GradeCodes)
            {
                if (existingByGrade.TryGetValue(gradeCode, out var existingClass))
                {
                    // A soft-deleted class keeps its (year, grade) pair reserved; skip that
                    // grade entirely rather than resurrecting admin-deleted data.
                    if (!existingClass.IsDeleted)
                    {
                        classesByGrade[gradeCode] = existingClass;
                    }

                    continue;
                }

                var academicClass = new AcademicClass
                {
                    AcademicYearId = academicYear.Id,
                    GradeCode = gradeCode,
                    Status = RecordStatus.Active
                };

                dbContext.AcademicClasses.Add(academicClass);
                classesByGrade[gradeCode] = academicClass;
            }

            await dbContext.SaveChangesAsync();
            return classesByGrade;
        }

        private static async Task<Dictionary<Guid, List<ClassSection>>> SeedSectionsAsync(
            ApplicationDbContext dbContext,
            Dictionary<string, AcademicClass> classesByGrade)
        {
            var classIds = new List<Guid>();
            foreach (var academicClass in classesByGrade.Values)
            {
                classIds.Add(academicClass.Id);
            }

            var existingSections = await dbContext.Set<ClassSection>()
                .IgnoreQueryFilters()
                .Where(s => classIds.Contains(s.AcademicClassId))
                .ToListAsync();

            var sectionsByClassId = new Dictionary<Guid, List<ClassSection>>();
            foreach (var academicClass in classesByGrade.Values)
            {
                sectionsByClassId[academicClass.Id] = new List<ClassSection>();
            }

            var existingSectionKeys = new HashSet<string>();
            foreach (var section in existingSections)
            {
                existingSectionKeys.Add(section.AcademicClassId + "|" + section.SectionCode);
                if (!section.IsDeleted)
                {
                    sectionsByClassId[section.AcademicClassId].Add(section);
                }
            }

            var seededSectionCodes = new[] { "A", "B" };
            foreach (var academicClass in classesByGrade.Values)
            {
                foreach (var sectionCode in seededSectionCodes)
                {
                    var sectionKey = academicClass.Id + "|" + sectionCode;
                    if (existingSectionKeys.Contains(sectionKey))
                    {
                        continue;
                    }

                    var section = new ClassSection
                    {
                        AcademicClassId = academicClass.Id,
                        SectionCode = sectionCode,
                        Capacity = SectionCapacity,
                        Status = RecordStatus.Active
                    };

                    dbContext.Set<ClassSection>().Add(section);
                    sectionsByClassId[academicClass.Id].Add(section);
                }
            }

            foreach (var sections in sectionsByClassId.Values)
            {
                sections.Sort(CompareSectionsByCode);
            }

            await dbContext.SaveChangesAsync();
            return sectionsByClassId;
        }

        private static int CompareSectionsByCode(ClassSection first, ClassSection second)
        {
            return string.CompareOrdinal(first.SectionCode, second.SectionCode);
        }

        private static Dictionary<string, string[]> BuildMandatorySubjectsByGrade()
        {
            var mandatorySubjects = new Dictionary<string, string[]>
            {
                ["NURSERY"] = new[] { "ENGLISH", "NEPALI", "MATH", "RHYMES", "DRAWING", "GENERAL_AWARENESS", "PLAY_ACTIVITIES", "MUSIC", "PHYSICAL_ACTIVITIES" },
                ["LKG"] = new[] { "ENGLISH", "NEPALI", "MATH", "ENV_AWARENESS", "DRAWING", "RHYMES", "MUSIC", "GAMES" },
                ["UKG"] = new[] { "ENGLISH", "NEPALI", "MATH", "EVS", "DRAWING", "GK", "MUSIC", "PHYSICAL_EDUCATION" },
                ["ONE"] = new[] { "ENGLISH", "NEPALI", "MATH", "OUR_SURROUNDINGS", "CREATIVE_ARTS", "HPE" },
                ["TWO"] = new[] { "ENGLISH", "NEPALI", "MATH", "OUR_SURROUNDINGS", "CREATIVE_ARTS", "HPE" },
                ["THREE"] = new[] { "ENGLISH", "NEPALI", "MATH", "SCIENCE_TECH", "SOCIAL_STUDIES", "CREATIVE_ARTS", "HPE" },
                ["FOUR"] = new[] { "ENGLISH", "NEPALI", "MATH", "SCIENCE_TECH", "SOCIAL_STUDIES", "CREATIVE_ARTS", "HPE" },
                ["FIVE"] = new[] { "ENGLISH", "NEPALI", "MATH", "SCIENCE_TECH", "SOCIAL_STUDIES", "CREATIVE_ARTS", "HPE" },
                ["SIX"] = new[] { "ENGLISH", "NEPALI", "MATH", "SCIENCE_TECH", "SOCIAL_HV", "HPCA", "LOCAL_SUBJECT" },
                ["SEVEN"] = new[] { "ENGLISH", "NEPALI", "MATH", "SCIENCE_TECH", "SOCIAL_HV", "HPCA", "LOCAL_SUBJECT" },
                ["EIGHT"] = new[] { "ENGLISH", "NEPALI", "MATH", "SCIENCE_TECH", "SOCIAL_HV", "HPCA", "LOCAL_SUBJECT" },
                ["NINE"] = new[] { "ENGLISH", "NEPALI", "MATH", "SCIENCE", "SOCIAL_STUDIES", "HPE" },
                ["TEN"] = new[] { "ENGLISH", "NEPALI", "MATH", "SCIENCE", "SOCIAL_STUDIES", "HPE" },
                ["ELEVEN"] = new[] { "ENGLISH", "NEPALI", "SOCIAL_LIFE_SKILLS" },
                ["TWELVE"] = new[] { "ENGLISH" }
            };

            return mandatorySubjects;
        }

        private static Dictionary<string, string[]> BuildOptionalSubjectsByGrade()
        {
            var facultySubjects = new[]
            {
                "PHYSICS", "CHEMISTRY", "BIOLOGY", "MATH",
                "ACCOUNTANCY", "ECONOMICS", "BUSINESS_STUDIES",
                "COMPUTER_SCIENCE", "SOCIOLOGY", "PSYCHOLOGY", "HOTEL_MANAGEMENT"
            };

            var optionalSubjects = new Dictionary<string, string[]>
            {
                ["NINE"] = new[] { "COMPUTER_SCIENCE", "OPT_MATHS", "OPT_ACCOUNT" },
                ["TEN"] = new[] { "COMPUTER_SCIENCE", "OPT_MATHS", "OPT_ACCOUNT" },
                ["ELEVEN"] = facultySubjects,
                ["TWELVE"] = facultySubjects
            };

            return optionalSubjects;
        }

        private static async Task<List<ClassSubject>> SeedClassSubjectsAsync(
            ApplicationDbContext dbContext,
            Dictionary<string, AcademicClass> classesByGrade)
        {
            var classIds = new List<Guid>();
            foreach (var academicClass in classesByGrade.Values)
            {
                classIds.Add(academicClass.Id);
            }

            var allClassSubjects = await dbContext.ClassSubjects
                .Where(cs => classIds.Contains(cs.AcademicClassId))
                .ToListAsync();

            // A subject appears either once class-wide or once per section, never both -- so a
            // (class, subject) pair existing in ANY scope means the seeder must leave it alone.
            var existingSubjectKeys = new HashSet<string>();
            foreach (var classSubject in allClassSubjects)
            {
                existingSubjectKeys.Add(classSubject.AcademicClassId + "|" + classSubject.SubjectCode);
            }

            var mandatorySubjectsByGrade = BuildMandatorySubjectsByGrade();
            var optionalSubjectsByGrade = BuildOptionalSubjectsByGrade();

            foreach (var gradeCode in GradeCodes)
            {
                if (!classesByGrade.TryGetValue(gradeCode, out var academicClass))
                {
                    continue;
                }

                var displayOrder = 1;
                foreach (var subjectCode in mandatorySubjectsByGrade[gradeCode])
                {
                    var created = AddClassSubjectIfMissing(dbContext, existingSubjectKeys, allClassSubjects, academicClass, subjectCode, true, displayOrder);
                    if (created)
                    {
                        displayOrder++;
                    }
                }

                if (optionalSubjectsByGrade.TryGetValue(gradeCode, out var optionalCodes))
                {
                    foreach (var subjectCode in optionalCodes)
                    {
                        var created = AddClassSubjectIfMissing(dbContext, existingSubjectKeys, allClassSubjects, academicClass, subjectCode, false, displayOrder);
                        if (created)
                        {
                            displayOrder++;
                        }
                    }
                }
            }

            await dbContext.SaveChangesAsync();
            return allClassSubjects;
        }

        private static bool AddClassSubjectIfMissing(
            ApplicationDbContext dbContext,
            HashSet<string> existingSubjectKeys,
            List<ClassSubject> allClassSubjects,
            AcademicClass academicClass,
            string subjectCode,
            bool isMandatory,
            int displayOrder)
        {
            var subjectKey = academicClass.Id + "|" + subjectCode;
            if (existingSubjectKeys.Contains(subjectKey))
            {
                return false;
            }

            var classSubject = new ClassSubject
            {
                AcademicClassId = academicClass.Id,
                SubjectCode = subjectCode,
                IsMandatory = isMandatory,
                DisplayOrder = displayOrder
            };

            dbContext.ClassSubjects.Add(classSubject);
            allClassSubjects.Add(classSubject);
            existingSubjectKeys.Add(subjectKey);
            return true;
        }

        private static async Task<List<Teacher>> SeedTeachersAsync(ApplicationDbContext dbContext)
        {
            var teacherFirstNames = new[]
            {
                "Ramesh", "Sita", "Hari", "Gita", "Binod",
                "Anita", "Krishna", "Sunita", "Prakash", "Mina",
                "Rajendra", "Kamala", "Suresh", "Laxmi", "Deepak",
                "Sarita", "Mohan", "Puja", "Narayan", "Rekha"
            };

            var teacherLastNames = new[]
            {
                "Adhikari", "Sharma", "Gautam", "Karki", "Thapa",
                "Shrestha", "Bhattarai", "Rai", "Koirala", "Gurung",
                "Poudel", "Bhandari", "Regmi", "Tamang", "Oli",
                "Joshi", "Khadka", "Basnet", "Subedi", "Magar"
            };

            var qualificationCodes = new[] { "MASTERS", "BACHELORS", "MASTERS", "PHD", "BACHELORS" };
            var qualificationCourses = new[]
            {
                "M.Ed. in Education", "B.Ed. in Education", "M.A. in English",
                "PhD in Education", "B.Sc. in Physical Science"
            };
            var institutions = new[]
            {
                "Tribhuvan University", "Kathmandu University",
                "Pokhara University", "Purbanchal University"
            };

            var existingEmployees = await dbContext.Employees
                .IgnoreQueryFilters()
                .Include(employee => employee.Teacher)
                .Where(employee => employee.Teacher != null)
                .ToListAsync();

            var existingByEmployeeCode = new Dictionary<string, Employee>();
            foreach (var employee in existingEmployees)
            {
                existingByEmployeeCode[employee.EmployeeCode] = employee;
            }

            var seededTeachers = new List<Teacher>();
            for (var teacherIndex = 0; teacherIndex < TeacherCount; teacherIndex++)
            {
                var employeeCode = "EMP2026" + (teacherIndex + 1).ToString("000");
                if (existingByEmployeeCode.TryGetValue(employeeCode, out var existingEmployee))
                {
                    if (!existingEmployee.IsDeleted)
                    {
                        seededTeachers.Add(existingEmployee.Teacher);
                    }

                    continue;
                }

                var firstName = teacherFirstNames[teacherIndex];
                var lastName = teacherLastNames[teacherIndex];
                var employeeEntity = new Employee
                {
                    EmployeeCode = employeeCode,
                    FirstName = firstName,
                    LastName = lastName,
                    Gender = teacherIndex % 2 == 0 ? Gender.Male : Gender.Female,
                    Email = (firstName + "." + lastName).ToLowerInvariant() + "@cmsapp.local",
                    Phone = "98" + (41000000 + teacherIndex + 1),
                    JoinDate = new DateTime(2015 + (teacherIndex % 10), (teacherIndex * 3) % 12 + 1, (teacherIndex * 7) % 28 + 1),
                    EmployeeCategoryCode = EmployeeCategoryCodes.Academic,
                    JobPositionCode = JobPositionCodes.Teacher,
                    EmploymentStatus = EmploymentStatus.Active,
                    PaymentMode = PaymentMode.BankDeposit
                };

                var teacherEntity = new Teacher
                {
                    Employee = employeeEntity
                };
                employeeEntity.Teacher = teacherEntity;

                var qualification = new TeacherQualification
                {
                    Teacher = teacherEntity,
                    QualificationCode = qualificationCodes[teacherIndex % qualificationCodes.Length],
                    CourseName = qualificationCourses[teacherIndex % qualificationCourses.Length],
                    Institution = institutions[teacherIndex % institutions.Length],
                    CompletionYear = 2005 + (teacherIndex % 15)
                };

                dbContext.Employees.Add(employeeEntity);
                dbContext.TeacherQualifications.Add(qualification);
                seededTeachers.Add(teacherEntity);
            }

            await dbContext.SaveChangesAsync();
            return seededTeachers;
        }

        private static async Task SeedTeacherAssignmentsAsync(
            ApplicationDbContext dbContext,
            List<Teacher> teachers,
            Dictionary<string, AcademicClass> classesByGrade,
            Dictionary<Guid, List<ClassSection>> sectionsByClassId,
            List<ClassSubject> classSubjects)
        {
            if (teachers.Count == 0)
            {
                return;
            }

            var classIds = new List<Guid>();
            foreach (var academicClass in classesByGrade.Values)
            {
                classIds.Add(academicClass.Id);
            }

            var existingAssignments = await dbContext.TeacherAssignments
                .Where(a => classIds.Contains(a.ClassSubject.AcademicClassId))
                .ToListAsync();

            var existingAssignmentKeys = new HashSet<string>();
            var sectionsWithClassTeacher = new HashSet<Guid>();
            foreach (var assignment in existingAssignments)
            {
                existingAssignmentKeys.Add(assignment.TeacherId + "|" + assignment.ClassSubjectId + "|" + assignment.ClassSectionId);
                if (assignment.IsClassTeacher && assignment.ClassSectionId.HasValue)
                {
                    sectionsWithClassTeacher.Add(assignment.ClassSectionId.Value);
                }
            }

            // Round-robin every class-wide subject row across the teacher pool.
            var subjectCounter = 0;
            for (var gradeIndex = 0; gradeIndex < GradeCodes.Length; gradeIndex++)
            {
                if (!classesByGrade.TryGetValue(GradeCodes[gradeIndex], out var academicClass))
                {
                    continue;
                }

                var classWideSubjects = GetClassWideSubjects(classSubjects, academicClass.Id);
                foreach (var classSubject in classWideSubjects)
                {
                    var teacher = teachers[subjectCounter % teachers.Count];
                    subjectCounter++;

                    var assignmentKey = teacher.Id + "|" + classSubject.Id + "|";
                    if (existingAssignmentKeys.Contains(assignmentKey))
                    {
                        continue;
                    }

                    var assignment = new TeacherAssignment
                    {
                        TeacherId = teacher.Id,
                        ClassSubjectId = classSubject.Id,
                        IsClassTeacher = false
                    };

                    dbContext.TeacherAssignments.Add(assignment);
                    existingAssignmentKeys.Add(assignmentKey);
                }

                // One class teacher per class, anchored to its first section and first subject.
                if (classWideSubjects.Count == 0)
                {
                    continue;
                }

                var sections = sectionsByClassId[academicClass.Id];
                if (sections.Count == 0)
                {
                    continue;
                }

                var classTeacherSection = sections[0];
                if (sectionsWithClassTeacher.Contains(classTeacherSection.Id))
                {
                    continue;
                }

                var classTeacher = teachers[gradeIndex % teachers.Count];
                var classTeacherSubject = classWideSubjects[0];
                var classTeacherKey = classTeacher.Id + "|" + classTeacherSubject.Id + "|" + classTeacherSection.Id;
                if (existingAssignmentKeys.Contains(classTeacherKey))
                {
                    continue;
                }

                var classTeacherAssignment = new TeacherAssignment
                {
                    TeacherId = classTeacher.Id,
                    ClassSubjectId = classTeacherSubject.Id,
                    ClassSectionId = classTeacherSection.Id,
                    IsClassTeacher = true
                };

                dbContext.TeacherAssignments.Add(classTeacherAssignment);
                existingAssignmentKeys.Add(classTeacherKey);
                sectionsWithClassTeacher.Add(classTeacherSection.Id);
            }

            await dbContext.SaveChangesAsync();
        }

        private static List<ClassSubject> GetClassWideSubjects(List<ClassSubject> classSubjects, Guid academicClassId)
        {
            var classWideSubjects = new List<ClassSubject>();
            foreach (var classSubject in classSubjects)
            {
                if (classSubject.AcademicClassId == academicClassId && classSubject.ClassSectionId == null)
                {
                    classWideSubjects.Add(classSubject);
                }
            }

            classWideSubjects.Sort(CompareClassSubjectsByDisplayOrder);
            return classWideSubjects;
        }

        private static int CompareClassSubjectsByDisplayOrder(ClassSubject first, ClassSubject second)
        {
            return first.DisplayOrder.CompareTo(second.DisplayOrder);
        }

        private static async Task SeedStudentsAsync(
            ApplicationDbContext dbContext,
            AcademicYear academicYear,
            Dictionary<string, AcademicClass> classesByGrade,
            Dictionary<Guid, List<ClassSection>> sectionsByClassId,
            List<ClassSubject> classSubjects)
        {
            var maleFirstNames = new[]
            {
                "Aarav", "Bibek", "Suman", "Nabin", "Kiran",
                "Roshan", "Sagar", "Dipesh", "Anish", "Pratik",
                "Sandesh", "Utsav", "Nischal", "Aayush", "Samir",
                "Bikash", "Rajan", "Milan", "Sujan", "Prabin"
            };

            var femaleFirstNames = new[]
            {
                "Aarati", "Bina", "Srijana", "Nisha", "Karuna",
                "Rojina", "Sabina", "Dikshya", "Anjali", "Pratiksha",
                "Sandhya", "Usha", "Nikita", "Aasha", "Samjhana",
                "Bipana", "Rachana", "Manisha", "Sushma", "Pabitra"
            };

            var familySurnames = new[]
            {
                "Adhikari", "Sharma", "Gautam", "Karki", "Thapa",
                "Shrestha", "Bhattarai", "Rai", "Koirala", "Gurung",
                "Poudel", "Bhandari", "Regmi", "Tamang", "Oli",
                "Joshi", "Khadka", "Basnet", "Subedi", "Magar",
                "Acharya", "Dahal", "Pandey", "Ghimire", "Lamichhane"
            };

            var occupations = new[]
            {
                "Agriculture", "Business", "Teacher", "Government Service",
                "Foreign Employment", "Driver", "Nurse", "Shopkeeper", "Engineer"
            };

            var cities = new[]
            {
                "Kathmandu", "Lalitpur", "Bhaktapur", "Pokhara",
                "Chitwan", "Butwal", "Biratnagar", "Dharan"
            };

            var electiveFaculties = new[]
            {
                new[] { "PHYSICS", "CHEMISTRY", "BIOLOGY" },
                new[] { "ACCOUNTANCY", "ECONOMICS", "BUSINESS_STUDIES" },
                new[] { "SOCIOLOGY", "PSYCHOLOGY", "COMPUTER_SCIENCE" }
            };

            var existingAdmissionNos = new HashSet<string>();
            var admissionNoRows = await dbContext.Students
                .IgnoreQueryFilters()
                .Select(s => s.AdmissionNo)
                .ToListAsync();
            foreach (var admissionNo in admissionNoRows)
            {
                existingAdmissionNos.Add(admissionNo);
            }

            var guardiansByEmail = new Dictionary<string, Guardian>();
            var existingGuardians = await dbContext.Guardians.ToListAsync();
            foreach (var guardian in existingGuardians)
            {
                if (!string.IsNullOrEmpty(guardian.Email))
                {
                    guardiansByEmail[guardian.Email] = guardian;
                }
            }

            var sectionIds = new List<Guid>();
            foreach (var sections in sectionsByClassId.Values)
            {
                foreach (var section in sections)
                {
                    sectionIds.Add(section.Id);
                }
            }

            var existingEnrollments = await dbContext.Enrollments
                .Where(e => sectionIds.Contains(e.ClassSectionId))
                .ToListAsync();

            var enrolledCountBySection = new Dictionary<Guid, int>();
            var nextRollBySection = new Dictionary<Guid, int>();
            foreach (var sectionId in sectionIds)
            {
                enrolledCountBySection[sectionId] = 0;
                nextRollBySection[sectionId] = 1;
            }

            foreach (var enrollment in existingEnrollments)
            {
                if (enrollment.Status == EnrollmentStatus.Enrolled)
                {
                    enrolledCountBySection[enrollment.ClassSectionId]++;
                }

                if (int.TryParse(enrollment.RollNumber, out var rollNumber) && rollNumber >= nextRollBySection[enrollment.ClassSectionId])
                {
                    nextRollBySection[enrollment.ClassSectionId] = rollNumber + 1;
                }
            }

            for (var studentIndex = 0; studentIndex < StudentCount; studentIndex++)
            {
                var admissionNo = "ADM2026" + (101 + studentIndex).ToString("000");
                if (existingAdmissionNos.Contains(admissionNo))
                {
                    continue;
                }

                var familyIndex = studentIndex / 2;
                var surname = familySurnames[familyIndex % familySurnames.Length];
                var gender = studentIndex % 2 == 0 ? Gender.Male : Gender.Female;
                var firstNamePool = gender == Gender.Male ? maleFirstNames : femaleFirstNames;
                var firstName = firstNamePool[(studentIndex * 7 + 3) % firstNamePool.Length];

                var gradeIndex = studentIndex % GradeCodes.Length;
                var gradeCode = GradeCodes[gradeIndex];
                var studentAge = 3 + gradeIndex + (studentIndex % 2);
                var city = cities[familyIndex % cities.Length];

                var student = new Student
                {
                    AdmissionNo = admissionNo,
                    FirstName = firstName,
                    LastName = surname,
                    Gender = gender,
                    DateOfBirth = new DateTime(academicYear.StartDate.Year - studentAge, studentIndex % 12 + 1, studentIndex % 28 + 1),
                    Address = city + "-" + (familyIndex % 15 + 1) + ", Nepal",
                    AdmissionDate = academicYear.StartDate,
                    Status = RecordStatus.Active
                };

                dbContext.Students.Add(student);
                existingAdmissionNos.Add(admissionNo);

                var father = EnsureGuardian(
                    dbContext,
                    guardiansByEmail,
                    maleFirstNames[familyIndex * 3 % maleFirstNames.Length],
                    surname,
                    "father." + surname.ToLowerInvariant() + (familyIndex + 1).ToString("00") + "@cmsapp.local",
                    "98" + (45000000 + familyIndex * 2),
                    occupations[familyIndex % occupations.Length],
                    city + ", Nepal");

                var mother = EnsureGuardian(
                    dbContext,
                    guardiansByEmail,
                    femaleFirstNames[familyIndex * 5 % femaleFirstNames.Length],
                    surname,
                    "mother." + surname.ToLowerInvariant() + (familyIndex + 1).ToString("00") + "@cmsapp.local",
                    "98" + (45000001 + familyIndex * 2),
                    occupations[(familyIndex + 3) % occupations.Length],
                    city + ", Nepal");

                var fatherLink = new StudentGuardian
                {
                    Student = student,
                    Guardian = father,
                    RelationshipCode = "FATHER",
                    IsPrimary = true
                };

                var motherLink = new StudentGuardian
                {
                    Student = student,
                    Guardian = mother,
                    RelationshipCode = "MOTHER",
                    IsPrimary = false
                };

                dbContext.StudentGuardians.Add(fatherLink);
                dbContext.StudentGuardians.Add(motherLink);

                if (!classesByGrade.TryGetValue(gradeCode, out var academicClass))
                {
                    continue;
                }

                var section = PickSectionWithRoom(sectionsByClassId[academicClass.Id], studentIndex / GradeCodes.Length, enrolledCountBySection);
                if (section == null)
                {
                    continue;
                }

                var enrollment = new Enrollment
                {
                    Student = student,
                    ClassSectionId = section.Id,
                    RollNumber = nextRollBySection[section.Id].ToString(),
                    EnrollmentDate = academicYear.StartDate,
                    Status = EnrollmentStatus.Enrolled
                };

                dbContext.Enrollments.Add(enrollment);
                nextRollBySection[section.Id]++;
                enrolledCountBySection[section.Id]++;

                // Vary electives by the student's ordinal WITHIN the grade (studentIndex / 15):
                // same-grade students differ by multiples of GradeCodes.Length, so any modulo
                // of studentIndex itself would pick the same elective for the whole grade.
                var withinGradeOrdinal = studentIndex / GradeCodes.Length;
                if (gradeCode == "NINE" || gradeCode == "TEN")
                {
                    var optionalSubjects = GetOptionalClassWideSubjects(classSubjects, academicClass.Id);
                    if (optionalSubjects.Count > 0)
                    {
                        var elective = optionalSubjects[withinGradeOrdinal % optionalSubjects.Count];
                        var enrollmentSubject = new EnrollmentSubject
                        {
                            Enrollment = enrollment,
                            ClassSubjectId = elective.Id
                        };

                        dbContext.EnrollmentSubjects.Add(enrollmentSubject);
                    }
                }
                else if (gradeCode == "ELEVEN" || gradeCode == "TWELVE")
                {
                    var facultyCodes = electiveFaculties[withinGradeOrdinal % electiveFaculties.Length];
                    foreach (var facultyCode in facultyCodes)
                    {
                        var elective = FindOptionalClassWideSubject(classSubjects, academicClass.Id, facultyCode);
                        if (elective == null)
                        {
                            continue;
                        }

                        var enrollmentSubject = new EnrollmentSubject
                        {
                            Enrollment = enrollment,
                            ClassSubjectId = elective.Id
                        };

                        dbContext.EnrollmentSubjects.Add(enrollmentSubject);
                    }
                }
            }

            await dbContext.SaveChangesAsync();
        }

        private static Guardian EnsureGuardian(
            ApplicationDbContext dbContext,
            Dictionary<string, Guardian> guardiansByEmail,
            string firstName,
            string lastName,
            string email,
            string phone,
            string occupation,
            string address)
        {
            if (guardiansByEmail.TryGetValue(email, out var existingGuardian))
            {
                return existingGuardian;
            }

            var guardian = new Guardian
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Phone = phone,
                Occupation = occupation,
                Address = address
            };

            dbContext.Guardians.Add(guardian);
            guardiansByEmail[email] = guardian;
            return guardian;
        }

        private static ClassSection PickSectionWithRoom(
            List<ClassSection> sections,
            int preferredIndex,
            Dictionary<Guid, int> enrolledCountBySection)
        {
            if (sections.Count == 0)
            {
                return null;
            }

            var startIndex = preferredIndex % sections.Count;
            for (var offset = 0; offset < sections.Count; offset++)
            {
                var section = sections[(startIndex + offset) % sections.Count];
                if (section.Capacity == 0 || enrolledCountBySection[section.Id] < section.Capacity)
                {
                    return section;
                }
            }

            return null;
        }

        private static List<ClassSubject> GetOptionalClassWideSubjects(List<ClassSubject> classSubjects, Guid academicClassId)
        {
            var optionalSubjects = new List<ClassSubject>();
            foreach (var classSubject in classSubjects)
            {
                if (classSubject.AcademicClassId == academicClassId && classSubject.ClassSectionId == null && !classSubject.IsMandatory)
                {
                    optionalSubjects.Add(classSubject);
                }
            }

            optionalSubjects.Sort(CompareClassSubjectsByDisplayOrder);
            return optionalSubjects;
        }

        private static ClassSubject FindOptionalClassWideSubject(List<ClassSubject> classSubjects, Guid academicClassId, string subjectCode)
        {
            foreach (var classSubject in classSubjects)
            {
                if (classSubject.AcademicClassId == academicClassId
                    && classSubject.ClassSectionId == null
                    && !classSubject.IsMandatory
                    && classSubject.SubjectCode == subjectCode)
                {
                    return classSubject;
                }
            }

            return null;
        }
    }
}
