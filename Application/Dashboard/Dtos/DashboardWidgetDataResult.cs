namespace Application.Dashboard.Dtos
{
    // What one IDashboardWidgetProvider hands back to the registry -- the registry fills in
    // WidgetCode/DisplayName (from the granted Menu row itself) to build the final
    // DashboardWidgetResultDto, so a provider only needs to describe its own outcome.
    public class DashboardWidgetDataResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}
