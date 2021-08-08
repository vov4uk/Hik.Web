using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using UltraFaceDotNet;

namespace DetectPeople.Face
{
    public class UltraFaceDetection : IDetectFace
    {
        private readonly UltraFaceParameter _param;

        public UltraFaceDetection()
        {
            try
            {
                var configuration = new Dictionary<string, string>();

                //_param = new UltraFaceParameter
                //{
                //    BinFilePath = configuration["Algos:UltraFace:BinFilePath"],
                //    ParamFilePath = configuration["Algos:UltraFace:ParamFilePath"],
                //    InputWidth = int.Parse(configuration["Algos:UltraFace:InputWidth"]),
                //    InputLength = int.Parse(configuration["Algos:UltraFace:InputLength"]),
                //    NumThread = int.Parse(configuration["Algos:UltraFace:NumThread"]),
                //    ScoreThreshold = 0.8f// float.Parse(configuration["Algos:UltraFace:ScoreThreshold"])
                //};


                _param = new UltraFaceParameter
                {
                    BinFilePath = "Classifiers/slim_320.bin",
                    InputLength = 240,
                    InputWidth = 320,
                    NumThread = 1,
                    ParamFilePath = "Classifiers/slim_320.param",
                    ScoreThreshold = 0.8f
                };

            }
            catch (Exception ex)
            {

            }
        }
        public Mat Detect(Mat mat)
        {
            using (var ultraFace = UltraFace.Create(_param))
            {
                using var inMat = NcnnDotNet.Mat.FromPixels(mat.Data, NcnnDotNet.PixelType.Bgr2Rgb, mat.Cols, mat.Rows);
                var faceInfos = ultraFace.Detect(inMat).ToArray();

                for (var j = 0; j < faceInfos.Length; j++)
                {
                    FaceInfo face = faceInfos[j];
                    mat.Rectangle(new Rect((int)face.X1, (int)face.Y1, (int)face.X2 - (int)face.X1, (int)face.Y2 - (int)face.Y1), Scalar.Red);
                }
                return mat;
            }
        }
    }
}
