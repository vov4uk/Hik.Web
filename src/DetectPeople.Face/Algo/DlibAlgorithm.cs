using FaceRecognitionDotNet;
using OpenCvSharp;
using System;
using System.Linq;

namespace DetectPeople.Face
{
    public class DlibAlgorithm : IDetectFace
    {
        private FaceRecognition _faceRecognition;
        public DlibAlgorithm()
        {
            _faceRecognition = FaceRecognition.Create(@"Classifiers\dlib-model\");
        }

        public Mat Detect(Mat mat)
        {
            using (var unknownImage = FaceRecognition.LoadImage(OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat)))
            {
                var faceLocations = _faceRecognition.FaceLocations(unknownImage, 0, Model.Hog).ToList();
                //var encodings = _faceRecognition.FaceEncodings(unknownImage, null, 1, PredictorModel.Large, Model.Hog);
                faceLocations.ForEach(r => mat.Rectangle(new Rect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top), Scalar.Red));
            }
            return mat;
        }
    }
}
