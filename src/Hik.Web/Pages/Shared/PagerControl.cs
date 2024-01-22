using JW;

namespace Hik.Web.Pages.Shared
{
    public class PagerControl : Pager
    {
        public string Url { get; private set; }
        public int Id { get; private set; }

        public string FormattedUrl => $"{Url}{Id}";

        public PagerControl(int id, string url, int totalItems, int currentPage = 1, int pageSize = 50, int maxPages = 10)
            : base(totalItems, currentPage, pageSize, maxPages)
        {
            Id = id;
            Url = url;
        }
    }
}
