using OpenCvSharp;

namespace DetectPeople.Face
{
    public interface IDetectFace
    {
        public Mat Detect(Mat mat);
    }
}
