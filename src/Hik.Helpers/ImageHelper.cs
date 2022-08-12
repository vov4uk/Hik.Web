using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Hik.Helpers.Abstraction;

namespace Hik.Helpers
{
    public class ImageHelper : IImageHelper
    {
        public byte[] GetThumbnail(string path, int width, int height)
        {
            byte[] bytes = null;
            if (!string.IsNullOrEmpty(path))
            {
                using Image image = Image.FromFile(path);
                using MemoryStream m = new MemoryStream();
                using Image thumb = image.GetThumbnailImage(width, height, () => false, IntPtr.Zero);
                thumb.Save(m, ImageFormat.Jpeg);
                bytes = new byte[m.Length];
                m.Position = 0;
                m.Read(bytes, 0, bytes.Length);
            }            
            return bytes;
        }

        public void SetDate(string path, string newPath, DateTime date)
        {
            if (!string.IsNullOrEmpty(path))
            {
                using Image image = Image.FromFile(path);
                var newItem = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
                newItem.Value = Encoding.ASCII.GetBytes(date.ToString("yyyy':'MM':'dd' 'HH':'mm':'ss"));
                newItem.Type = 2;
                newItem.Id = 306;
                image.SetPropertyItem(newItem);
                image.Save(newPath, image.RawFormat);
            }
        }
    }
}
