namespace Application.Guardians.Queries
{
    // Search matches First/Last name; Phone is a dedicated filter since a guardian's name and
    // phone are usually looked up independently. FromDate/ToDate compare against CreatedTs.
    public class GetGuardiansQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Search { get; set; }
        public string Phone { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
