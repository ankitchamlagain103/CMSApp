using Application.Dashboard.Dtos;

namespace Application.Dashboard
{
    // One implementation per dashboard widget, registered in DI (Application.DependencyInjection)
    // and resolved by WidgetCode against a granted Menu.Code by IDashboardWidgetRegistryService.
    // A provider never computes new data -- it adapts an existing feature service's own composite
    // endpoint (IDashboardService.GetAccountsSummaryAsync, IEmployeeService.GetMyDashboardAsync,
    // ...) into the generic widget envelope, so there is exactly one implementation of each
    // widget's actual logic, reused by both its dedicated endpoint and this generic one.
    public interface IDashboardWidgetProvider
    {
        // Must match a Menu.Code seeded with IsDashboardWidget = true (see MenuSeeder).
        string WidgetCode { get; }

        Task<DashboardWidgetDataResult> GetDataAsync(int take, CancellationToken cancellationToken = default);
    }
}
