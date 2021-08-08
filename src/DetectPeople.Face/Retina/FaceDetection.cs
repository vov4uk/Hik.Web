using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;
using Retina;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace DetectPeople.Face.Retina
{
    public class FaceDetection
    {
        public float Coeficient { get; set; } = 50.0f;
        public bool IsSDD { get; set; }

        OpenCvSharp.Dnn.Net detector;
        public FaceDetection()
        {
            face.Init();
        }

        RetinaFace face = new RetinaFace();
        FsaDetector fsa = new FsaDetector();

        public RectangleF[] GetFacesSSD(Mat img)
        {
            var sz = img.Size();
            var crop = img.Resize(new OpenCvSharp.Size(300, 300));

            var blob = OpenCvSharp.Dnn.CvDnn.BlobFromImage(crop, 1, new OpenCvSharp.Size(300, 300), new Scalar(104, 177, 123), false);

            detector.SetInput(blob);
            var detections = detector.Forward();

            int[] dims = new int[detections.Dims];
            for (int i = 0; i < detections.Dims; i++)
            {
                var dim = detections.Total(i, i + 1); ;
                dims[i] = (int)dim;
            }
            var confidence = 0.7;

            List<RectangleF> ret = new List<RectangleF>();
            for (int i = 0; i < dims[2]; i++)
            {
                var score = detections.At<float>(0, 0, i, 2);

                if (score > confidence)
                {
                    float[] box = new float[4];
                    for (int j = 0; j < 4; j++)
                    {
                        var b1 = detections.At<float>(0, 0, i, j + 3);
                        box[j] = b1;
                    }

                    box[0] *= sz.Width;
                    box[1] *= sz.Height;
                    box[2] *= sz.Width;
                    box[3] *= sz.Height;
                    Rect rect = new Rect((int)box[0], (int)box[1], (int)(box[2] - box[0]), (int)(box[3] - box[1]));
                    var b = new Rect(0, 0, img.Cols, img.Rows) & rect;
                    if (b == rect)
                    {
                        ret.Add(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height));
                    }
                }
            }
            return ret.ToArray();
        }

        private List<FaceInfo> Detect(string path)
        {
            string[] exts = { "jpg", "png", "bmp" };
            if (exts.Any(z => path.EndsWith(z)))
            {
                var mat = Cv2.ImRead(path);
                using var session = new InferenceSession("Classifiers\\FaceDetector.onnx");
                return ProcessMat(mat, session);
            }
            return new List<FaceInfo>();
        }

        public List<FaceInfo> ProcessMat(Mat mat, InferenceSession session)
        {
            List<FaceInfo> faceInfos = new List<FaceInfo>();
            RectangleF[] faces = null;

            Tuple<RectangleF[], Point2f[][], float[]> ret = null;

            if (IsSDD)
            {
                faces = GetFacesSSD(mat.Clone());
            }
            else
            {
                ret = face.Forward(mat.Clone(), session);
                faces = ret.Item1;
            }

            foreach (RectangleF item in faces)
            {
                //fix rect to quad
                float inflx = 0;
                float infly = 0;
                var orig = new Rect((int)item.X, (int)item.Y, (int)item.Width, (int)item.Height);

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

                bool is_inside = (corrected & new Rect(0, 0, mat.Cols, mat.Rows)) == corrected;
                if (is_inside)
                {
                    var cor = mat.Clone(corrected);
                    var axis = fsa.GetAxis(cor.Clone(), corrected);
                    var val = Math.Abs(axis.Item2[0]) + Math.Abs(axis.Item2[1]);
                    Console.WriteLine(val);
                    if(val > Coeficient)
                    faceInfos.Add(new FaceInfo() { Label = val.ToString(), Mat = mat, Rect = new Rect2f(item.X, item.Y, item.Width, item.Height) });
                }
            }

            if (faceInfos.Any())
            {
                return faceInfos.OrderBy(x => x.Rect.Left).ThenBy(z => z.Rect.Top).ToList();
            }

            return new();
        }

        public (RecFaceInfo, RecFaceInfo, double?) RecognizeFace(Mat cor)
        {
            var fn = new FaceNet();
            return fn.Recognize(cor.Clone());
        }


        private void DrawRectangle(Graphics gr, RectangleF item)
        {
            int faceBoxWidth = 3;
            gr.DrawRectangle(new Pen(Color.Yellow, faceBoxWidth), new Rectangle((int)item.X, (int)item.Y, (int)item.Width, (int)item.Height));
        }

        private void DrawLandms(Graphics gr, Tuple<RectangleF[], Point2f[][], float[]> ret)
        {
            for (int i = 0; i < ret.Item3.Length; i++)
            {
                float item = (float)ret.Item3[i];
                var pos = ret.Item1[i];
                gr.DrawString(Math.Round(item, 4) + "", new Font("Arial", 12), Brushes.Yellow, pos.X, pos.Y - 30);
            }
        }


        bool inited = false;
        public List<FaceInfo> OpenFile(string FileName)
        {
            InitSessions();
            return Detect(FileName);
        }


        public void InitSessions()
        {
            if (inited) return;
            inited = true;
            try
            {
                fsa.Init();

                var config_path = "Classifiers\\resnet10_ssd.prototxt";
                var face_model_path = "Classifiers\\res10_300x300_ssd_iter_140000.caffemodel";

                detector = OpenCvSharp.Dnn.CvDnn.ReadNetFromCaffe(config_path, face_model_path);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        public class FaceInfo
        {
            public string Label;
            public Mat Mat;
            public Rect2f Rect;
        }
    }
}
