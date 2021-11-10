using DetectPeople.Face.Helpers;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DetectPeople.Face.Retina
{
    //Fine-Grained Structure Aggregation
    public class HeadPoseDetector
    {
        InferenceSession session1;
        InferenceSession session2;
        public void Init()
        {
            session1 = new InferenceSession(Path.Combine(PathHelper.BinPath, "Assets", "fsanet-1x1-iter-688590.onnx"));
            session2 = new InferenceSession(Path.Combine(PathHelper.BinPath, "Assets", "fsanet-var-iter-688590.onnx"));
        }
        public int[][] Get_axis(float yaw, float pitch, float roll, float tdx, float tdy, float size = 50)
        {

            List<int[]> ret = new List<int[]>();
            pitch = (float)(pitch * Math.PI / 180f);
            yaw = (float)-(yaw * Math.PI / 180f);
            roll = (float)(roll * Math.PI / 180f);


            var x1 = size * (Math.Cos(yaw) * Math.Cos(roll)) + tdx;
            var y1 = size * (Math.Cos(pitch) * Math.Sin(roll) + Math.Cos(roll) * Math.Sin(pitch) * Math.Sin(yaw)) + tdy;

            var x2 = size * (-Math.Cos(yaw) * Math.Sin(roll)) + tdx;
            var y2 = size * (Math.Cos(pitch) * Math.Cos(roll) - Math.Sin(pitch) * Math.Sin(yaw) * Math.Sin(roll)) + tdy;

            var x3 = size * Math.Sin(yaw) + tdx;
            var y3 = size * (-Math.Cos(yaw) * Math.Sin(pitch)) + tdy;


            ret.Add(new int[] { (int)tdx, (int)tdy, (int)x1, (int)y1 });
            ret.Add(new int[] { (int)tdx, (int)tdy, (int)x2, (int)y2 });
            ret.Add(new int[] { (int)tdx, (int)tdy, (int)x3, (int)y3 });

            return ret.ToArray();
        }
        public HeadPoseDetectorResult GetAxis(Mat mat, Rect rect)
        {
            var a1 = CalcAxis(session1, mat);
            var a2 = CalcAxis(session2, mat);
            var yaw = (a1[0] + a2[0]) / 2;
            var pitch = (a1[1] + a2[1]) / 2;
            var roll = (a1[2] + a2[2]) / 2;


            var tdx = rect.X + rect.Width / 2;
            var tdy = rect.Y + rect.Height / 2;
            return new HeadPoseDetectorResult(Get_axis(yaw, pitch, roll, tdx, tdy), yaw, pitch, roll);
        }
        float[] CalcAxis(InferenceSession session, Mat mat)
        {
            mat.ConvertTo(mat, MatType.CV_32F);
            mat = mat.Resize(new Size(64, 64));
            mat -= 127.5f;
            mat /= 128f;

            var res2 = mat.Split();
            res2[0].GetArray(out float[] ret1);
            res2[1].GetArray(out float[] ret2);
            res2[2].GetArray(out float[] ret3);

            var inputMeta = session.InputMetadata;
            var container = new List<NamedOnnxValue>();

            foreach (var name in inputMeta.Keys)
            {
                var inputData = new float[ret1.Length + ret2.Length + ret3.Length];

                Array.Copy(ret1, 0, inputData, 0, ret1.Length);
                Array.Copy(ret2, 0, inputData, ret1.Length, ret2.Length);
                Array.Copy(ret3, 0, inputData, ret1.Length + ret2.Length, ret3.Length);
                var tensor = new DenseTensor<float>(inputData, inputMeta[name].Dimensions);

                container.Add(NamedOnnxValue.CreateFromTensor(name, tensor));
            }

            using (var results = session.Run(container))
            {
                var data = results.First().AsTensor<float>();
                var rets = data.ToArray();
                var yaw = rets[0];
                var pitch = rets[1];
                var roll = rets[2];
                return new float[] { yaw, pitch, roll };
            }
        }
    }
}
