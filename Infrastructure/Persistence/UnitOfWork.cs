using Application.Common.Interfaces;
using Domain.Interfaces;
using Infrastructure.Persistence.Repositories;

namespace Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;
        private IMenuRepository _menuRepository;
        private IConfigTypeRepository _configTypeRepository;
        private IConfigRepository _configRepository;
        private ISystemAccessLogRepository _systemAccessLogRepository;
        private IErrorLogRepository _errorLogRepository;
        private IAppConfigRepository _appConfigRepository;
        private IAcademicYearRepository _academicYearRepository;
        private IAcademicClassRepository _academicClassRepository;
        private ITeacherRepository _teacherRepository;
        private IGuardianRepository _guardianRepository;
        private IStudentRepository _studentRepository;
        private IEnrollmentRepository _enrollmentRepository;
        private IFeeStructureRepository _feeStructureRepository;
        private IFeeRuleRepository _feeRuleRepository;
        private IFeeInvoiceRepository _feeInvoiceRepository;
        private IFeeGenerationRunRepository _feeGenerationRunRepository;
        private IPayrollRunRepository _payrollRunRepository;
        private IFiscalYearRepository _fiscalYearRepository;
        private IEmployeeRepository _employeeRepository;

        private IDocumentTemplateRepository _documentTemplateRepository;
        private ICalendarConfigRepository _calendarConfigRepository;
        private ICalendarEventRepository _calendarEventRepository;
        private IMeetingRepository _meetingRepository;

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IMenuRepository Menus
        {
            get
            {
                if (_menuRepository == null)
                {
                    _menuRepository = new MenuRepository(_dbContext);
                }

                return _menuRepository;
            }
        }

        public IConfigTypeRepository ConfigTypes
        {
            get
            {
                if (_configTypeRepository == null)
                {
                    _configTypeRepository = new ConfigTypeRepository(_dbContext);
                }

                return _configTypeRepository;
            }
        }

        public IConfigRepository Configs
        {
            get
            {
                if (_configRepository == null)
                {
                    _configRepository = new ConfigRepository(_dbContext);
                }

                return _configRepository;
            }
        }

        public ISystemAccessLogRepository SystemAccessLogs
        {
            get
            {
                if (_systemAccessLogRepository == null)
                {
                    _systemAccessLogRepository = new SystemAccessLogRepository(_dbContext);
                }

                return _systemAccessLogRepository;
            }
        }

        public IErrorLogRepository ErrorLogs
        {
            get
            {
                if (_errorLogRepository == null)
                {
                    _errorLogRepository = new ErrorLogRepository(_dbContext);
                }

                return _errorLogRepository;
            }
        }

        public IAppConfigRepository AppConfigs
        {
            get
            {
                if (_appConfigRepository == null)
                {
                    _appConfigRepository = new AppConfigRepository(_dbContext);
                }

                return _appConfigRepository;
            }
        }

        public IAcademicYearRepository AcademicYears
        {
            get
            {
                if (_academicYearRepository == null)
                {
                    _academicYearRepository = new AcademicYearRepository(_dbContext);
                }

                return _academicYearRepository;
            }
        }

        public IAcademicClassRepository AcademicClasses
        {
            get
            {
                if (_academicClassRepository == null)
                {
                    _academicClassRepository = new AcademicClassRepository(_dbContext);
                }

                return _academicClassRepository;
            }
        }

        public ITeacherRepository Teachers
        {
            get
            {
                if (_teacherRepository == null)
                {
                    _teacherRepository = new TeacherRepository(_dbContext);
                }

                return _teacherRepository;
            }
        }

        public IGuardianRepository Guardians
        {
            get
            {
                if (_guardianRepository == null)
                {
                    _guardianRepository = new GuardianRepository(_dbContext);
                }

                return _guardianRepository;
            }
        }

        public IStudentRepository Students
        {
            get
            {
                if (_studentRepository == null)
                {
                    _studentRepository = new StudentRepository(_dbContext);
                }

                return _studentRepository;
            }
        }

        public IEnrollmentRepository Enrollments
        {
            get
            {
                if (_enrollmentRepository == null)
                {
                    _enrollmentRepository = new EnrollmentRepository(_dbContext);
                }

                return _enrollmentRepository;
            }
        }

        public IFeeStructureRepository FeeStructures
        {
            get
            {
                if (_feeStructureRepository == null)
                {
                    _feeStructureRepository = new FeeStructureRepository(_dbContext);
                }

                return _feeStructureRepository;
            }
        }

        public IFeeRuleRepository FeeRules
        {
            get
            {
                if (_feeRuleRepository == null)
                {
                    _feeRuleRepository = new FeeRuleRepository(_dbContext);
                }

                return _feeRuleRepository;
            }
        }

        public IFeeInvoiceRepository FeeInvoices
        {
            get
            {
                if (_feeInvoiceRepository == null)
                {
                    _feeInvoiceRepository = new FeeInvoiceRepository(_dbContext);
                }

                return _feeInvoiceRepository;
            }
        }

        public IFeeGenerationRunRepository FeeGenerationRuns
        {
            get
            {
                if (_feeGenerationRunRepository == null)
                {
                    _feeGenerationRunRepository = new FeeGenerationRunRepository(_dbContext);
                }

                return _feeGenerationRunRepository;
            }
        }

        public IPayrollRunRepository PayrollRuns
        {
            get
            {
                if (_payrollRunRepository == null)
                {
                    _payrollRunRepository = new PayrollRunRepository(_dbContext);
                }

                return _payrollRunRepository;
            }
        }

        public IFiscalYearRepository FiscalYears
        {
            get
            {
                if (_fiscalYearRepository == null)
                {
                    _fiscalYearRepository = new FiscalYearRepository(_dbContext);
                }

                return _fiscalYearRepository;
            }
        }

        public IEmployeeRepository Employees
        {
            get
            {
                if (_employeeRepository == null)
                {
                    _employeeRepository = new EmployeeRepository(_dbContext);
                }

                return _employeeRepository;
            }
        }

        public IDocumentTemplateRepository DocumentTemplates
        {
            get
            {
                if (_documentTemplateRepository == null)
                {
                    _documentTemplateRepository = new DocumentTemplateRepository(_dbContext);
                }

                return _documentTemplateRepository;
            }
        }

        public ICalendarConfigRepository CalendarConfigs
        {
            get
            {
                if (_calendarConfigRepository == null)
                {
                    _calendarConfigRepository = new CalendarConfigRepository(_dbContext);
                }

                return _calendarConfigRepository;
            }
        }

        public ICalendarEventRepository CalendarEvents
        {
            get
            {
                if (_calendarEventRepository == null)
                {
                    _calendarEventRepository = new CalendarEventRepository(_dbContext);
                }

                return _calendarEventRepository;
            }
        }

        public IMeetingRepository Meetings
        {
            get
            {
                if (_meetingRepository == null)
                {
                    _meetingRepository = new MeetingRepository(_dbContext);
                }

                return _meetingRepository;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);
            return affectedRows;
        }
    }
}
