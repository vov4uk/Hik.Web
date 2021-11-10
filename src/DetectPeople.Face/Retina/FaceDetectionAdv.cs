using DetectPeople.Face.Helpers;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace DetectPeople.Face.Retina
{
    public class FaceDetectionAdv
    {
        const double Rad2Deg = 180.0 / Math.PI;
        private static readonly Brush[] eyeBrushes = new Brush[]
        {
            Brushes.Red,
            Brushes.Yellow,
            Brushes.Green,
            Brushes.Blue,
            Brushes.Violet
        };
        private static readonly Pen[] headPosePens = new Pen[] { Pens.Red, Pens.Green, Pens.Blue };

        private static readonly int eyeWidth = 8;
        private static readonly int faceBoxWidth = 3;
        private static readonly int yprShift = 0;
        private InferenceSession session;
        private readonly float expandKoef = 1.1f;
        private readonly HeadPoseDetector headPoseDetector = new HeadPoseDetector();
        private bool inited = false;
        private readonly RetinaFaceDetection retinaFaceDetect = new RetinaFaceDetection();
        public FaceDetectionAdv()
        {
        }

        public List<FaceInfo> DetectFaces(string path)
        {
            InitSessions();
            string[] exts = { "jpg", "png", "bmp" };
            if (exts.Any(z => path.EndsWith(z)))
            {
                var mat = Cv2.ImRead(path);
                return Detect(mat, session);
            }
            return new List<FaceInfo>();
        }

        public List<FaceInfo> Detect(Mat inputImg, InferenceSession session)
        {
            List<FaceInfo> faceInfos = new List<FaceInfo>();

            RetinaFaceDetectionResult result = retinaFaceDetect.Detect(inputImg.Clone(), session);
            Rect[] faces = result.FaceRectangles;

            for (int j = 0; j < faces.Length; j++)
            {
                Rect orig = faces[j];

                //fix rect to quad
                float inflx = 0;
                float infly = 0;

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

                OpenCvSharp.Point center = new OpenCvSharp.Point(orig.Left + orig.Width / 2, orig.Top + orig.Height / 2);
                Rect corrected = new Rect((int)(center.X - ww / 2), (int)(center.Y - hh / 2), (int)ww, (int)hh);

                bool is_inside = (corrected & new Rect(0, 0, inputImg.Cols, inputImg.Rows)) == corrected;
                if (is_inside)
                {
                    var cor = inputImg.Clone(corrected);

                    HeadPoseDetectorResult headPoseResult = headPoseDetector.GetAxis(cor.Clone(), corrected);
                    float val = Math.Abs(headPoseResult.Yaw) + Math.Abs(headPoseResult.Pitch);

                    FaceInfo faceInfo = new FaceInfo
                    {
                        Mat = inputImg.Clone(),
                        Label = val,
                        FaceRectangle = orig,
                        Head = headPoseResult,
                        EyePoints = result.EyePoints[j],
                        EyePointsScore = result.EyePointsScores[j]
                    };
                    faceInfos.Add(faceInfo);
                }
            }

            return faceInfos.OrderBy(x => x.FaceRectangle.Left).ThenBy(z => z.FaceRectangle.Top).ToList();
        }

        public void InitSessions()
        {
            if (inited) return;
            inited = true;
            try
            {
                headPoseDetector.Init();
                retinaFaceDetect.Init();
                session = new InferenceSession(Path.Combine(PathHelper.BinPath, "Assets", "FaceDetector.onnx"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static void SaveFaceImg(FaceInfo face, Mat inputImg, string outputPath)
        {
            var inputBmp = BitmapConverter.ToBitmap(inputImg);
            var outputImg = Graphics.FromImage(inputBmp);
            var head = face.Head;
            Rect orig = face.FaceRectangle;

            // Eye points
            for (int i = 0; i < face.EyePoints.Length; i++)
            {
                Point2f point = face.EyePoints[i];
                outputImg.FillEllipse(eyeBrushes[i], point.X - eyeWidth / 2, point.Y - eyeWidth / 2, eyeWidth, eyeWidth);
            }

            //Eye score 
            outputImg.DrawString(Math.Round(face.EyePointsScore, 4) + "", new Font("Arial", 12), Brushes.Yellow, orig.X, orig.Y - 30);

            //Face rectangle
            outputImg.DrawRectangle(new Pen(Color.Green, faceBoxWidth * 2), new Rectangle(orig.X, orig.Y, orig.Width, orig.Height));

            // YRP
            outputImg.DrawString("yaw:   " + head.Yaw,   new Font("Arial", 12), Brushes.White, orig.X + yprShift, orig.Y);
            outputImg.DrawString("pitch: " + head.Pitch, new Font("Arial", 12), Brushes.White, orig.X + yprShift, orig.Y + 16);
            outputImg.DrawString("roll:  " + head.Roll,  new Font("Arial", 12), Brushes.White, orig.X + yprShift, orig.Y + 32);

            //headPose

            for (int i = 0; i < head.Axis.Length; i++)
            {
                int[] axis = head.Axis[i];
                int x1 = axis[0];
                int y1 = axis[1];
                int x2 = axis[2];
                int y2 = axis[3];
                outputImg.DrawLine(new Pen(headPosePens[i].Color, faceBoxWidth), x1, y1, x2, y2);
                outputImg.DrawString($"{headPosePens[i].Color}:   {Angle(new System.Drawing.Point(x1, y1), new System.Drawing.Point(x2, y2))}",
                    new Font("Arial", 12),
                    Brushes.White,
                    orig.X + yprShift,
                    orig.Y + 48 + i * 12);
            }
            BitmapConverter.ToMat(inputBmp).SaveImage(outputPath);
        }

        public static int Angle(System.Drawing.Point start, System.Drawing.Point end)
        {
            return (int)(Math.Atan2(start.Y - end.Y, end.X - start.X) * Rad2Deg);
        }

        public static int Angle(int[] axis)
        {
            int x1 = axis[0];
            int y1 = axis[1];
            int x2 = axis[2];
            int y2 = axis[3];
            return (int)(Math.Atan2(y1 - y2, x2 - x1) * Rad2Deg);
        }
    }
}
