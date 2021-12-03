using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Image;
using System.Drawing;

namespace DetectPeople.YOLOv5.DataStructures
{
    public class BitmapData
    {
        [ColumnName("bitmap")]
        [ImageType(640, 640)]
        public Bitmap Image { get; set; }

        [ColumnName("width")]
        public float ImageWidth => Image.Width;

        [ColumnName("height")]
        public float ImageHeight => Image.Height;
    }
}
