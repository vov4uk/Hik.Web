using OpenCvSharp;

namespace DetectPeople.Face.Retina
{
    public class FaceInfo
    {
        public float Label { get; set; }
        public Mat Mat { get; set; }
        public Rect FaceRectangle { get; set; }

        public Point[] EyePoints { get; set; }
        public float EyePointsScore { get; set; }

        public HeadPoseDetectorResult Head { get; set; }
    }
}
