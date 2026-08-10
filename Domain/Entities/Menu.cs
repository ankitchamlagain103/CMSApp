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

        // Whether this menu shows up as a curated tile in the dashboard's "Quick Access" grid
        // (2026-07-28) -- deliberately explicit rather than inferred from MenuType/Url, so an
        // admin can curate exactly which pages appear there via POST/PUT /api/menus.
        public bool IsQuickLink { get; set; }

        // Whether this menu is registered as a generic dashboard widget (2026-08-07) -- when
        // true, GET /api/dashboard/widgets includes it (by Code) for any caller whose role grants
        // it and whose audience matches, resolving its data through a registered
        // IDashboardWidgetProvider keyed on the same Code. Same "admin curates via POST/PUT
        // /api/menus" reasoning as IsQuickLink -- a menu becomes a widget by flag, not inference.
        public bool IsDashboardWidget { get; set; }
        public virtual Menu MainMenu { get; set; }
        public virtual ICollection<Menu> Childrens { get; set; } = new List<Menu>();
    }
}
