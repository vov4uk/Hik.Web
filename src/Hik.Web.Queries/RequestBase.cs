namespace Hik.Web.Queries
{
    public abstract class RequestBase
    {

        public int PageSize { get; set; }

        public int MaxPages { get; set; }

        public int CurrentPage { get; set; }
    }
}
