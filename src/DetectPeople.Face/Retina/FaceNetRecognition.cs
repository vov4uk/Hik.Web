using DetectPeople.Face.Helpers;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DetectPeople.Face.Retina
{
    public class FaceNetRecognition
    {
        public static List<FaceRecognitionResult> Faces = new List<FaceRecognitionResult>();
        private static InferenceSession session;

        public static float[] EncodeImg(Mat inputImg)
        {
            if (session == null)
            {
                session = new InferenceSession(Path.Combine(PathHelper.BinPath, "Assets", "facenet.onnx"));
            }

            var inputMeta = session.InputMetadata;
            var container = new List<NamedOnnxValue>();

            foreach (var name in inputMeta.Keys)
            {
                inputImg = inputImg.Resize(new Size(inputMeta[name].Dimensions[2], inputMeta[name].Dimensions[1]));
                inputImg.At<Vec3b>(0, 0);
                inputImg.ConvertTo(inputImg, MatType.CV_32F);

                Mat mean = new Mat();
                Mat std = new Mat();
                var zzz = inputImg.Reshape(1, 160 * 160);
                zzz.MeanStdDev(mean, std);

                var mm = inputImg.Mean();
                var _mean = mean.Mean().Val0;
                var _std = std.Mean().Val0;

                var res2 = inputImg.Split();

                res2[0] -= _mean;
                res2[1] -= _mean;
                res2[2] -= _mean;

                res2[0] /= _std;
                res2[1] /= _std;
                res2[2] /= _std;
                Cv2.Merge(res2, inputImg);
                inputImg.GetArray(out Vec3f[] ret1);
                var inputData = new float[ret1.Length * 3];
                for (int i = 0; i < ret1.Length; i++)
                {
                    inputData[i * 3] = ret1[i].Item0;
                    inputData[i * 3 + 1] = ret1[i].Item1;
                    inputData[i * 3 + 2] = ret1[i].Item2;
                }

                inputMeta[name].Dimensions[0] = 1;
                var tensor = new DenseTensor<float>(inputData, inputMeta[name].Dimensions);
                container.Add(NamedOnnxValue.CreateFromTensor(name, tensor));
            }

            using (var results = session.Run(container))
            {
                var resultArray = results.ToArray();
                var tensor = resultArray[0].AsTensor<float>();
                return tensor.ToArray();
            }
        }

        public FaceNetRecognitionResult Recognize(Mat input)
        {
            var encoding = EncodeImg(input);
            FaceRecognitionResult recognitionResult = new FaceRecognitionResult() { Encoding = encoding };
            double? maxdist = null;
            FaceRecognitionResult best = null;
            foreach (var item in Faces)
            {
                var dist = item.CosDist(recognitionResult);
                if (maxdist == null || maxdist.Value < dist)
                {
                    maxdist = dist;
                    best = item;
                }
            }
            return new FaceNetRecognitionResult(recognitionResult, best, maxdist);
        }
    }
}