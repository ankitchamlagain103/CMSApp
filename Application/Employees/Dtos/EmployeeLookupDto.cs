namespace Application.Employees.Dtos
{
    // Deliberately minimal -- backs GET /api/employees/lookup, a DefaultEnabledMenu-gated
    // search-by-name/code endpoint any authenticated user can call (picking a Manager or a leave
    // Substitute). The full EmployeeDto/GetEmployees carries PAN/SSF/CIT/bank-account numbers and
    // is permission-gated behind EMPLOYEE_LIST for that reason -- this DTO exists specifically so
    // that gate doesn't have to be bypassed just to run an autocomplete search.
    public class EmployeeLookupDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string EmployeeCode { get; set; }
        public string JobPositionCode { get; set; }
        public string JobPositionLabel { get; set; }
    }
}
