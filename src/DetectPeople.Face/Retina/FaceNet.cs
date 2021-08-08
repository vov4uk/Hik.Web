using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System.Collections.Generic;
using System.Linq;

namespace Retina
{
    public class FaceNet
    {
        static InferenceSession  session;
        public static List<RecFaceInfo> Faces = new List<RecFaceInfo>();
        public (RecFaceInfo, RecFaceInfo, double?) Recognize(Mat mat)
        {
            var res = Inference(mat);
            RecFaceInfo rr = new RecFaceInfo() { Embedding = res };
            double? maxdist = null;
            RecFaceInfo best = null;
            foreach (var item in Faces)
            {
                var dist = item.CosDist(rr);
                if (maxdist == null || maxdist.Value < dist)
                {
                    maxdist = dist;
                    best = item;
                }
            }
            return (rr, best, maxdist);
        }
        public static float[] Inference(Mat mat)
        {
            if (session == null)
            {
                session = new InferenceSession("Classifiers\\facenet.onnx");
            }

            var inputMeta = session.InputMetadata;
            var container = new List<NamedOnnxValue>();

            foreach (var name in inputMeta.Keys)
            {
                mat = mat.Resize(new Size(inputMeta[name].Dimensions[2], inputMeta[name].Dimensions[1]));
                var v1 = mat.At<Vec3b>(0, 0);

                mat.ConvertTo(mat, MatType.CV_32F);
                Mat mean = new Mat();
                Mat std = new Mat();
                var zzz = mat.Reshape(1, 160 * 160);
                zzz.MeanStdDev(mean, std);

                var mm = mat.Mean();
                var _mean = mean.Mean().Val0;
                var _std = std.Mean().Val0;

                var res2 = mat.Split();

                res2[0] -= _mean;
                res2[1] -= _mean;
                res2[2] -= _mean;

                res2[0] /= _std;
                res2[1] /= _std;
                res2[2] /= _std;
                Cv2.Merge(res2, mat);
                mat.GetArray(out Vec3f[] ret1);
                var inputData = new float[ret1.Length * 3];
                for (int i = 0; i < ret1.Length; i++)
                {
                    inputData[i * 3] = ret1[i].Item0;
                    inputData[i * 3 + 1] = ret1[i].Item1;
                    inputData[i * 3 + 2] = ret1[i].Item2;
                }


                inputMeta[name].Dimensions[0] = 1;
                var tensor = new DenseTensor<float>(inputData, inputMeta[name].Dimensions);
                container.Add(NamedOnnxValue.CreateFromTensor<float>(name, tensor));
            }


            using (var results = session.Run(container))
            {
                var rrrr = results.ToArray();
                var data1 = rrrr[0].AsTensor<float>();
                return data1.ToArray();
            }
        }
    }
}