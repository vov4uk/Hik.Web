using System.Drawing;

namespace DetectPeople.YOLOv5.DataStructures
{
    public class ObjectDetectResult
    {
        public ObjectDetectResult(int id, float[] bbox, string label, float confidence)
        {
            Id = id;
            BBox = bbox;
            Label = label;
            Confidence = confidence;
        }

        /// <summary>
        /// x1, y1, x2, y2 in page coordinates.
        /// <para>left, top, right, bottom.</para>
        /// </summary>
        public float[] BBox { get; }

        /// <summary>
        /// Confidence level.
        /// </summary>
        public float Confidence { get; }

        public int Id { get; }

        /// <summary>
        /// The Bbox category.
        /// </summary>
        public string Label { get; }

        public Rectangle GetRectangle()
        {
            var x1 = (int)BBox[0];
            var y1 = (int)BBox[1];
            var x2 = (int)BBox[2];
            var y2 = (int)BBox[3];
            var H = y2 - y1;
            var W = x2 - x1;

            return new Rectangle(x1, y1, W, H);
        }
    }
}
