namespace Hik.Web.Queries
{
    public abstract class ResponseBase<T> : IHandlerResult
        where T : class
    {
        public IReadOnlyCollection<T> Items { get; set; }

        public int TotalItems { get; set; }
    }
}
