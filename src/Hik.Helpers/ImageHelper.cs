using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection.Emit;
using Hik.Helpers.Abstraction;


namespace Hik.Helpers
{
    public class ImageHelper : IImageHelper
    {
        private static byte[] DefaultPoster;

        static ImageHelper()
        {
            DefaultPoster = Convert.FromBase64String("/9j/4QAYRXhpZgAASUkqAAgAAAAAAAAAAAAAAP/hAylodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvADw/eHBhY2tldCBiZWdpbj0i77u/IiBpZD0iVzVNME1wQ2VoaUh6cmVTek5UY3prYzlkIj8+IDx4OnhtcG1ldGEgeG1sbnM6eD0iYWRvYmU6bnM6bWV0YS8iIHg6eG1wdGs9IkFkb2JlIFhNUCBDb3JlIDUuMC1jMDYwIDYxLjEzNDc3NywgMjAxMC8wMi8xMi0xNzozMjowMCAgICAgICAgIj4gPHJkZjpSREYgeG1sbnM6cmRmPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5LzAyLzIyLXJkZi1zeW50YXgtbnMjIj4gPHJkZjpEZXNjcmlwdGlvbiByZGY6YWJvdXQ9IiIgeG1sbnM6eG1wPSJodHRwOi8vbnMuYWRvYmUuY29tL3hhcC8xLjAvIiB4bWxuczp4bXBNTT0iaHR0cDovL25zLmFkb2JlLmNvbS94YXAvMS4wL21tLyIgeG1sbnM6c3RSZWY9Imh0dHA6Ly9ucy5hZG9iZS5jb20veGFwLzEuMC9zVHlwZS9SZXNvdXJjZVJlZiMiIHhtcDpDcmVhdG9yVG9vbD0iQWRvYmUgUGhvdG9zaG9wIENTNSBXaW5kb3dzIiB4bXBNTTpJbnN0YW5jZUlEPSJ4bXAuaWlkOjhERDQ3MkYwM0Q4QTExRTNBODk2OUREMDM0OTM5NkJDIiB4bXBNTTpEb2N1bWVudElEPSJ4bXAuZGlkOjhERDQ3MkYxM0Q4QTExRTNBODk2OUREMDM0OTM5NkJDIj4gPHhtcE1NOkRlcml2ZWRGcm9tIHN0UmVmOmluc3RhbmNlSUQ9InhtcC5paWQ6OERENDcyRUUzRDhBMTFFM0E4OTY5REQwMzQ5Mzk2QkMiIHN0UmVmOmRvY3VtZW50SUQ9InhtcC5kaWQ6OERENDcyRUYzRDhBMTFFM0E4OTY5REQwMzQ5Mzk2QkMiLz4gPC9yZGY6RGVzY3JpcHRpb24+IDwvcmRmOlJERj4gPC94OnhtcG1ldGE+IDw/eHBhY2tldCBlbmQ9InIiPz7/2wBDAAEBAQEBAQEBAQEBAQEBAQIBAQEBAQIBAQECAgICAgICAgIDAwQDAwMDAwICAwQDAwQEBAQEAgMF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");
        }

        public byte[] GetThumbnail(string path, int width, int height)
        {
            byte[] bytes = null;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                using Image image = Image.FromFile(path);
                using MemoryStream m = new MemoryStream();
                using Image thumb = image.GetThumbnailImage(width, height, () => false, IntPtr.Zero);
                thumb.Save(m, ImageFormat.Jpeg);
                bytes = new byte[m.Length];
                m.Position = 0;
                m.Read(bytes, 0, bytes.Length);
            }
            return bytes ?? DefaultPoster;
        }

        public void SetDate(string path, DateTime date)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path) && Path.GetExtension(path) == ".jpg")
            {
                using (var tfile = new TagLib.Jpeg.File(path, TagLib.ReadStyle.PictureLazy))
                {
                    tfile.GetTag(TagLib.TagTypes.XMP, true);
                    tfile.GetTag(TagLib.TagTypes.TiffIFD, true);
                    tfile.ImageTag.DateTime = date;
                    tfile.Save();
                }
            }
        }

        public string GetDescriptionData(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path) && Path.GetExtension(path) == ".jpg")
            {
                using (var tfile = new TagLib.Jpeg.File(path, TagLib.ReadStyle.PictureLazy))
                {
                    if (!string.IsNullOrEmpty(tfile.ImageTag.Comment))
                    {
                        return tfile.ImageTag.Comment;
                    }
                }
            }

            return null;
        }
    }
}
