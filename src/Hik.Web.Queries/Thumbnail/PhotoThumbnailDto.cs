namespace Hik.Web.Queries.Thumbnail
{
    public class PhotoThumbnailDto : IHandlerResult
    {
        public byte[] Poster { get; set; }
        public int Id { get; set; }
    }
}
