namespace Hik.Web.Queries.Search
{
    public class SearchTriggersDto : IHandlerResult
    {
        public Dictionary<int, string> Triggers { get; set; }
    }
}
