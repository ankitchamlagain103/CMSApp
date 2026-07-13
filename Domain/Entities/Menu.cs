namespace Domain.Entities
{
    public class Menu : SoftDeleteAuditableEntity
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string DisplayName { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public string MenuType { get; set; } // MAIN_MENU , SUB_MENU AND PERMISSION
        public string Controller { get; set; }
        public string Action { get; set; }
        public int? ParentId { get; set; }
        public string MenuFor { get; set; } // ADMIN, USER, BOTH
        public int Order { get; set; }
        public bool IsHidden { get; set; }
        public virtual Menu MainMenu { get; set; }
        public virtual ICollection<Menu> Childrens { get; set; } = new List<Menu>();
    }
}
