using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Point = OpenCvSharp.Point;

namespace DetectPeople.Face.Retina
{
    public class RetinaFaceDetection
    {
        readonly List<float[]> prior_data = new List<float[]>();

        public RetinaFaceDetectionResult Detect(Mat inputImg, InferenceSession session)
        {
            var origWidth = inputImg.Width;
            var origHeight = inputImg.Height;

            var inputMeta = session.InputMetadata;
            var container = new List<NamedOnnxValue>();

            foreach (var name in inputMeta.Keys)
            {
                inputImg = inputImg.Resize(new OpenCvSharp.Size(inputMeta[name].Dimensions[3], inputMeta[name].Dimensions[2]));
                inputImg.ConvertTo(inputImg, MatType.CV_32F);

                var res2 = inputImg.Split();
                res2[0] -= 104;
                res2[1] -= 117;
                res2[2] -= 123;
                res2[0].GetArray<float>(out float[] ret1);
                res2[1].GetArray<float>(out float[] ret2);
                res2[2].GetArray<float>(out float[] ret3);

                var inputData = new float[ret1.Length + ret2.Length + ret3.Length];

                Array.Copy(ret1, 0, inputData, 0, ret1.Length);
                Array.Copy(ret2, 0, inputData, ret1.Length, ret2.Length);
                Array.Copy(ret3, 0, inputData, ret1.Length + ret2.Length, ret3.Length);

                var tensor = new DenseTensor<float>(inputData, inputMeta[name].Dimensions);
                container.Add(NamedOnnxValue.CreateFromTensor<float>(name, tensor));
            }

            List<float[]> loc = new List<float[]>();
            List<float> scores = new List<float>();
            List<float[]> _landms = new List<float[]>();
            float[] variances = new float[] { 0.1f, 0.2f };
            var nnInputWidth = 576;
            var nnInputHeight = 288;
            float wz1 = nnInputWidth;
            float hz1 = nnInputHeight;
            float[] scale = new float[] { (float)nnInputWidth, (float)nnInputHeight, (float)nnInputWidth, (float)nnInputHeight };
            float koef = wz1 / (float)(origWidth);
            float koef2 = hz1 / (float)(origHeight);

            float[] resize = new float[] { koef, koef2 };

            using (var results = session.Run(container))
            {
                var rrrr = results.ToArray();
                var data1 = rrrr[0].AsTensor<float>();
                var rets1 = data1.ToArray();
                var data2 = rrrr[1].AsTensor<float>();
                var rets3 = data2.ToArray();
                var data3 = rrrr[2].AsTensor<float>();
                var rets2 = data3.ToArray();

                for (var i = 0; i < rets1.Length; i += 4)
                {
                    loc.Add(new float[] { rets1[i + 0], rets1[i + 1], rets1[i + 2], rets1[i + 3] });
                }

                for (var i = 0; i < rets2.Length; i += 10)
                {
                    _landms.Add(new float[]{
                        rets2[i + 0],rets2[i + 1],rets2[i + 2],rets2[i + 3],rets2[i + 4],
                        rets2[i + 5],rets2[i + 6],rets2[i + 7],rets2[i + 8],rets2[i + 9] });
                }

                for (var i = 0; i < rets3.Length; i += 2)
                {
                    scores.Add(rets3[i + 1]);
                }
            }

            var boxes = Decode(loc, prior_data, variances);
            for (var i = 0; i < boxes.Count(); i++)
            {
                boxes[i][0] = (boxes[i][0] * scale[0]) / resize[0];
                boxes[i][1] = (boxes[i][1] * scale[1]) / resize[1];
                boxes[i][2] = (boxes[i][2] * scale[2]) / resize[0];
                boxes[i][3] = (boxes[i][3] * scale[3]) / resize[1];
            }
            //todo!!!!OpenCvSharp.Dnn.CvDnn.NMSBoxes()

            var landms = Decode_landm(_landms, prior_data, variances);
            float[] scale1 = new float[] { wz1, hz1, wz1, hz1, wz1, hz1, wz1, hz1, wz1, hz1 };

            for (var i = 0; i < landms.Count(); i++)
            {
                landms[i][0] = (landms[i][0] * scale1[0]) / resize[0];
                landms[i][1] = (landms[i][1] * scale1[1]) / resize[1];
                landms[i][2] = (landms[i][2] * scale1[2]) / resize[0];
                landms[i][3] = (landms[i][3] * scale1[3]) / resize[1];
                landms[i][4] = (landms[i][4] * scale1[4]) / resize[0];
                landms[i][5] = (landms[i][5] * scale1[5]) / resize[1];
                landms[i][6] = (landms[i][6] * scale1[6]) / resize[0];
                landms[i][7] = (landms[i][7] * scale1[7]) / resize[1];
                landms[i][8] = (landms[i][8] * scale1[8]) / resize[0];
                landms[i][9] = (landms[i][9] * scale1[9]) / resize[1];
            }

            float confidence_threshold = 0.02f;
            List<int> inds = new List<int>();

            for (var i = 0; i < scores.Count(); i++)
            {
                if (scores[i] > confidence_threshold)
                {
                    inds.Add(i);
                }
            }

            List<float[]> boxes2 = new List<float[]>();
            for (var i = 0; i < inds.Count(); i++)
            {
                boxes2.Add(boxes[inds[i]]);
            }
            boxes = boxes2.ToArray();
            List<float[]> landms2 = new List<float[]>();
            for (var i = 0; i < inds.Count(); i++)
            {
                landms2.Add(landms[inds[i]]);
            }

            landms = landms2.ToArray();
            List<float> scores2 = new List<float>();
            for (var i = 0; i < inds.Count(); i++)
            {
                scores2.Add(scores[inds[i]]);
            }
            scores = scores2;
            var order = Sort_indexes(scores);
            List<float[]> boxes3 = new List<float[]>();
            for (var i = 0; i < order.Count(); i++)
            {
                boxes3.Add(boxes[order[i]]);
            }

            boxes = boxes3.ToArray();

            List<float[]> landms3 = new List<float[]>();
            for (var i = 0u; i < order.Count(); i++)
            {
                landms3.Add(landms[order[i]]);
            }

            landms = landms3.ToArray();

            ///////////
            List<float> scores3 = new List<float>();
            for (var i = 0u; i < order.Count(); i++)
            {
                scores3.Add(scores[order[i]]);
            }

            scores = scores3;
            //2. nms
            List<float[]> dets = new List<float[]>();
            for (var i = 0; i < boxes.Count(); i++)
            {
                dets.Add(new float[] { boxes[i][0], boxes[i][1], boxes[i][2], boxes[i][3], scores[i] });
            }
            var keep = Py_cpu_nms(dets, 0.4f);

            List<float[]> dets2 = new List<float[]>();

            for (var i = 0u; i < keep.Count(); i++)
            {
                dets2.Add(dets[keep[i]]);
            }
            dets = dets2;


            List<float[]> landms4 = new List<float[]>();

            for (var i = 0; i < keep.Count(); i++)
            {
                landms4.Add(landms[keep[i]]);
            }
            landms = landms4.ToArray();

            List<Rect> detections = new List<Rect>();
            float vis_thresh = 0.5f;

            List<int> indexMap = new List<int>();
            List<Point[]> olandms = new List<Point[]>();
            List<float[]> odets = new List<float[]>();
            List<float> oscores = new List<float>();

            for (var i = 0; i < dets.Count(); i++)
            {
                var aa = dets[i];
                if (aa[4] < vis_thresh) continue;
                detections.Add(new Rect((int)aa[0], (int)aa[1], (int)(aa[2] - aa[0]), (int)(aa[3] - aa[1])));
                indexMap.Add(i);

                List<Point> l = new List<Point>();
                for (int j = 0; j < landms[i].Length; j += 2)
                {
                    l.Add(new Point(landms[i][j], landms[i][j + 1]));
                }
                olandms.Add(l.ToArray());
                oscores.Add(scores3[i]);
            }


            for (var i = 0; i < dets.Count(); i++)
            {
                odets.Add(dets[i]);
            }

            return new RetinaFaceDetectionResult() { FaceRectangles = detections.ToArray(), EyePoints = olandms.ToArray(), EyePointsScores = oscores.ToArray() };
        }

        public void Init(int w = 576, int h = 288)
        {
            PriorBoxes(w, h);
        }

        public int[] Sort_indexes(List<float> scores)
        {
            var order = scores.Select((z, i) => new Tuple<int, float>(i, z)).OrderByDescending(z => z.Item2).Select(z => z.Item1).ToArray();
            return order;
        }

        float[][] Decode(List<float[]> loc, List<float[]> priors, float[] variances)
        {
            List<float[]> ret = new List<float[]>();

            for (var i = 0; i < loc.Count; i++)
            {

                float z0 = priors[i][0] + loc[i][0] * variances[0] * priors[i][2];
                float z1 = priors[i][1] + loc[i][1] * variances[0] * priors[i][3];
                float z2 = (float)(priors[i][2] * Math.Exp(loc[i][2] * variances[1]));
                float z3 = (float)(priors[i][3] * Math.Exp(loc[i][3] * variances[1]));

                z0 -= z2 / 2;
                z1 -= z3 / 2;
                z2 += z0;
                z3 += z1;
                ret.Add(new float[] { z0, z1, z2, z3 });
            }


            return ret.ToArray();
        }

        float[][] Decode_landm(List<float[]> pre, List<float[]> priors, float[] variances)
        {

            List<float[]> ret = new List<float[]>();

            for (var i = 0; i < pre.Count; i++)
            {
                float x0 = priors[i][0] + pre[i][0] * variances[0] * priors[i][2];
                float y0 = priors[i][1] + pre[i][1] * variances[0] * priors[i][3];

                float x1 = priors[i][0] + pre[i][2] * variances[0] * priors[i][2];
                float y1 = priors[i][1] + pre[i][3] * variances[0] * priors[i][3];


                float x2 = priors[i][0] + pre[i][4] * variances[0] * priors[i][2];
                float y2 = priors[i][1] + pre[i][5] * variances[0] * priors[i][3];

                float x3 = priors[i][0] + pre[i][6] * variances[0] * priors[i][2];
                float y3 = priors[i][1] + pre[i][7] * variances[0] * priors[i][3];

                float x4 = priors[i][0] + pre[i][8] * variances[0] * priors[i][2];
                float y4 = priors[i][1] + pre[i][9] * variances[0] * priors[i][3];

                ret.Add(new float[] { x0, y0, x1, y1, x2, y2, x3, y3, x4, y4 });
            }

            return ret.ToArray();
        }

        void PriorBoxes(int img_w = 576, int img_h = 288)
        {
            int[][] min_sizes = new int[][] { new int[] { 16, 32 }, new int[] { 64, 128 }, new int[] { 256, 512 } };
            int[] steps = new int[] { 8, 16, 32 };

            List<int[]> feature_maps = new List<int[]>();

            foreach (var step in steps)
            {
                int w1 = (int)Math.Ceiling(img_w / (float)step);
                int h1 = (int)Math.Ceiling(img_h / (float)step);
                feature_maps.Add(new int[] { h1, w1 });
            }

            for (var k = 0; k < feature_maps.Count; k++)
            {
                var f = feature_maps[k];
                var _min_sizes = min_sizes[k];
                for (var i = 0u; i < (uint)f[0]; i++)
                {
                    for (int j = 0; j < f[1]; j++)
                    {

                        for (var jj = 0u; jj < 2; jj++)
                        {
                            int min_size = _min_sizes[jj];
                            float s_kx = min_size / (float)img_w;
                            float s_ky = min_size / (float)img_h;
                            List<float> dense_cx = new List<float>();
                            List<float> dense_cy = new List<float>();
                            float x = j + 0.5f;
                            dense_cx.Add(x * steps[k] / img_w);
                            float y = i + 0.5f;
                            dense_cy.Add(y * steps[k] / img_h);

                            foreach (var cy in dense_cy)
                            {
                                foreach (var cx in dense_cx)
                                {
                                    prior_data.Add(new float[] { cx, cy, s_kx, s_ky });
                                }
                            }
                        }
                    }
                }
            }
        }

        int[] Py_cpu_nms(List<float[]> dets, float thresh)
        {
            List<float> x1 = new List<float>();
            List<float> y1 = new List<float>();
            List<float> x2 = new List<float>();
            List<float> y2 = new List<float>();
            List<float> scores = new List<float>();

            List<float> areas = new List<float>();
            for (var i = 0; i < dets.Count; i++)
            {
                x1.Add(dets[i][0]);
                y1.Add(dets[i][1]);
                x2.Add(dets[i][2]);
                y2.Add(dets[i][3]);
                scores.Add(dets[i][4]);
                areas.Add((x2[i] - x1[i] + 1) * (y2[i] - y1[i] + 1));
            }

            var order = scores.Select((z, i) => new Tuple<int, float>(i, z)).OrderByDescending(z => z.Item2).Select(z => z.Item1).ToArray();

            List<int> keep = new List<int>();
            while (order.Count() > 0)
            {
                int i = order[0];
                keep.Add(i);
                List<float> xx1 = new List<float>();
                List<float> yy1 = new List<float>();
                List<float> xx2 = new List<float>();
                List<float> yy2 = new List<float>();
                for (var j = 1; j < order.Count(); j++)
                {
                    xx1.Add(Math.Max(x1[i], x1[order[j]]));
                    yy1.Add(Math.Max(y1[i], y1[order[j]]));
                    xx2.Add(Math.Min(x2[i], x2[order[j]]));
                    yy2.Add(Math.Min(y2[i], y2[order[j]]));
                }
                List<float> w = new List<float>();
                List<float> h = new List<float>();
                List<float> inter = new List<float>();

                for (var j = 0; j < xx2.Count(); j++)
                {
                    w.Add(Math.Max(0.0f, xx2[j] - xx1[j] + 1));
                    h.Add(Math.Max(0.0f, yy2[j] - yy1[j] + 1));
                    inter.Add(w[j] * h[j]);
                }

                List<float> ovr = new List<float>();
                for (var j = 0; j < inter.Count(); j++)
                {
                    ovr.Add(inter[j] / (areas[i] + areas[order[j + 1]] - inter[j]));
                }

                List<int> inds = new List<int>();
                for (var j = 0; j < ovr.Count(); j++)
                {
                    if (ovr[j] > thresh) continue;
                    inds.Add(j);
                }

                List<int> order2 = new List<int>();
                for (var j = 0; j < inds.Count(); j++)
                {
                    order2.Add(order[inds[j] + 1]);
                }

                order = order2.ToArray();
            }

            return keep.ToArray();
        }
    }
}