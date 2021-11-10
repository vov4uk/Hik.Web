using OpenCvSharp;
using Point = OpenCvSharp.Point;

namespace DetectPeople.Face.Retina
{
    public class RetinaFaceDetectionResult
    {
        public Rect[] FaceRectangles { get; set; }
        public Point[][] EyePoints { get; set; }
        public float[] EyePointsScores { get; set; }
    }
}
