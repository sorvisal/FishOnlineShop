namespace FishOnlineShop.ViewModels
{
    /// <summary>
    /// Input for <c>Shared/_Pagination.cshtml</c>. Page numbers and gaps are worked out
    /// here so the partial stays free of presentation logic.
    /// </summary>
    public class PaginationViewModel
    {
        private int _page = 1;
        private int _totalPages = 1;

        public int Page
        {
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }

        public int TotalPages
        {
            get => _totalPages;
            set => _totalPages = value < 1 ? 1 : value;
        }

        public int TotalItems { get; set; }

        /// <summary>Existing query string values that must survive a page change.</summary>
        public Dictionary<string, string?> RouteValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Window of pages around the current one, null entries mean an ellipsis.</summary>
        public IReadOnlyList<int?> Pages => BuildPages();

        public int FirstItem => TotalItems == 0 ? 0 : ((Page - 1) * PageSize) + 1;

        public int LastItem => Math.Min(Page * PageSize, TotalItems);

        public int PageSize { get; set; } = 12;

        public bool HasPrevious => Page > 1;

        public bool HasNext => Page < TotalPages;

        public bool HasMultiplePages => TotalPages > 1;

        private IReadOnlyList<int?> BuildPages()
        {
            const int window = 2;
            var pages = new List<int?>();

            for (var number = 1; number <= TotalPages; number++)
            {
                var isEdge = number == 1 || number == TotalPages;
                var isNearCurrent = number >= Page - window && number <= Page + window;

                if (isEdge || isNearCurrent)
                {
                    pages.Add(number);
                }
                else if (pages[^1] is not null)
                {
                    // Collapse the skipped stretch into a single ellipsis.
                    pages.Add(null);
                }
            }

            return pages;
        }
    }
}