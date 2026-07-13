namespace Domain.Common
{
    public class PagedResult<TEntity>
    {
        public IReadOnlyList<TEntity> Items { get; set; }
        public int TotalCount { get; set; }
    }
}
