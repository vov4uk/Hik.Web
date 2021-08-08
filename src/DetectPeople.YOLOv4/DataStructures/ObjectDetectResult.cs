namespace DetectPeople.YOLOv4.DataStructures
{
    public class ObjectDetectResult
    {
        public int Id { get; }

        /// <summary>
        /// x1, y1, x2, y2 in page coordinates.
        /// <para>left, top, right, bottom.</para>
        /// </summary>
        public float[] BBox { get; }

        /// <summary>
        /// The Bbox category.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Confidence level.
        /// </summary>
        public float Confidence { get; }

        public ObjectDetectResult(int id, float[] bbox, string label, float confidence)
        {
            Id = id;
            BBox = bbox;
            Label = label;
            Confidence = confidence;
        }
    }
}
