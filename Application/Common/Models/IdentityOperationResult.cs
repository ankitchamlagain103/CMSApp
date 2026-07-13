namespace Application.Common.Models
{
    public class IdentityOperationResult
    {
        public bool Succeeded { get; set; }
        public string UserId { get; set; }
        public IEnumerable<string> Errors { get; set; } = new List<string>();
    }
}
