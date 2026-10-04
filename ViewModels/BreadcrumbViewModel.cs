namespace FishOnlineShop.ViewModels
{
    public class BreadcrumbItem
    {
        public BreadcrumbItem()
        {
        }

        /// <summary>Creates a link crumb pointing at a controller action.</summary>
        public BreadcrumbItem(string text, string controllerName, string actionName, Dictionary<string, string?>? routeValues = null)
        {
            Text = text;
            ControllerName = controllerName;
            ActionName = actionName;
            RouteValues = routeValues ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Creates the current (non clickable) crumb.</summary>
        public BreadcrumbItem(string text)
        {
            Text = text;
            ControllerName = null;
            ActionName = null;
        }

        public string Text { get; set; } = string.Empty;

        /// <summary>Controller for this crumb. Null together with ActionName means "current page".</summary>
        public string? ControllerName { get; set; }

        public string? ActionName { get; set; }

        public Dictionary<string, string?> RouteValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public bool IsCurrent => string.IsNullOrEmpty(ControllerName) || string.IsNullOrEmpty(ActionName);
    }

    /// <summary>Input for <c>Shared/_Breadcrumb.cshtml</c>.</summary>
    public class BreadcrumbViewModel
    {
        public BreadcrumbViewModel()
        {
        }

        public BreadcrumbViewModel(params BreadcrumbItem[] items)
        {
            Items = items;
        }

        public IReadOnlyList<BreadcrumbItem> Items { get; set; } = Array.Empty<BreadcrumbItem>();
    }
}