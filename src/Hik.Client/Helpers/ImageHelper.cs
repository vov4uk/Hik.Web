using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Serialization;
using System.Text;
using Hik.Client.Abstraction;

namespace Hik.Client.Helpers
{
    public class ImageHelper : IImageHelper
    {
        public void SetDate(string path, string newPath, DateTime date)
        {
            using (Image image = Image.FromFile(path))
            {
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
