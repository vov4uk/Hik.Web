using DetectPeople.Face.Helpers;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace DetectPeople.Face.Retina
{
    public partial class FaceDetection
    {
        private readonly RetinaFaceDetection retinaFaceDetector = new RetinaFaceDetection();
        private bool inited = false;
        private InferenceSession session;
        public FaceDetection()
        {
        }

        public List<FaceInfo> DetectFaces(string path, bool isInside = true)
        {
            InitSessions();
            string[] exts = { "jpg", "png", "bmp" };
            if (exts.Any(z => path.EndsWith(z)))
            {
                var mat = Cv2.ImRead(path);
                return Detect(mat, session, isInside);
            }
            return new List<FaceInfo>();
        }

        private void InitSessions()
        {
            if (inited) return;
            inited = true;

            session = new InferenceSession(Path.Combine(PathHelper.BinPath, "Assets", "FaceDetector.onnx"));
            retinaFaceDetector.Init();
        }

        private List<FaceInfo> Detect(Mat inputImg, InferenceSession session, bool isInside)
        {
            List<FaceInfo> faceInfos = new List<FaceInfo>();

            RetinaFaceDetectionResult ret = retinaFaceDetector.Detect(inputImg.Clone(), session);
            Rect[] faces = ret.FaceRectangles;

            foreach (Rect orig in faces)
            {
                var newFace = new FaceInfo() { Mat = inputImg, FaceRectangle = orig };
                if (isInside)
                {
                    float inflx = 0;
                    float infly = 0;

                    float expandKoef = 1.1f;
                    OpenCvSharp.Point center = new OpenCvSharp.Point(orig.Left + orig.Width / 2, orig.Top + orig.Height / 2);

                    float ww = orig.Width * expandKoef;
                    float hh = orig.Height * expandKoef;

                    if (ww > hh)
                    {
                        infly = (int)(ww - hh);
                    }
                    else
                    {
                        inflx = (int)(hh - ww);
                    }
                    ww += inflx;
                    hh += infly;

                    Rect corrected = new Rect((int)(center.X - ww / 2), (int)(center.Y - hh / 2), (int)ww, (int)hh);

                    bool is_inside = (corrected & new Rect(0, 0, inputImg.Cols, inputImg.Rows)) == corrected;
                    if (is_inside)
                    {
                        faceInfos.Add(newFace);
                    }
                }
                else
                {
                    faceInfos.Add(newFace);
                }
            }

            return faceInfos.OrderBy(x => x.FaceRectangle.Left).ThenBy(z => z.FaceRectangle.Top).ToList(); ;
        }
    }
}