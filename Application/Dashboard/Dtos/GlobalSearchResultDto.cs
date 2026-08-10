namespace Application.Dashboard.Dtos
{
    // GET /api/dashboard/global-search's payload -- the navbar "Ctrl+K"-style search across
    // Students and Employees (which includes teaching staff, see GlobalSearchEmployeeResultDto's
    // own comment). Grouped by entity type, each capped at the caller's `limit`, rather than one
    // flat interleaved list -- lets the UI render "Students" / "Employees" sections the way the
    // screenshot's search box implies, without re-sorting a mixed list itself.
    public class GlobalSearchResultDto
    {
        public string Query { get; set; }
        public List<GlobalSearchStudentResultDto> Students { get; set; } = new List<GlobalSearchStudentResultDto>();
        public List<GlobalSearchEmployeeResultDto> Employees { get; set; } = new List<GlobalSearchEmployeeResultDto>();
    }
}
