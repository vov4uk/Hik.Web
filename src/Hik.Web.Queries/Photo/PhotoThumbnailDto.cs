namespace Hik.Web.Queries.Photo
{
    public class PhotoThumbnailDto : IHandlerResult
    {
        public byte[] Poster { get; set; }
        public int Id { get; set; }
    }
}
