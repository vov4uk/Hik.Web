using OpenCvSharp;
using System;

namespace DetectPeople.Face.Retina
{
    public class FaceRecognitionResult
    {
        public float[] Encoding { get; set; }
        public Mat Face { get; set; }
        public string Label { get; set; }

        private double? _norm2;
        public string FilePath { get; set; }
        public double CosDist(FaceRecognitionResult f)
        {
            var n1 = Norm2();
            var n2 = f.Norm2();
            double dot = 0;
            for (int i = 0; i < Encoding.Length; i++)
            {
                dot += Encoding[i] * f.Encoding[i];
            }
            return dot / n1 / n2;
        }

        public double Norm2()
        {
            if (_norm2 != null) return _norm2.Value;

            double ret = 0;
            foreach (var item in Encoding)
            {
                ret += item * item;
            }
            ret = Math.Sqrt(ret);
            _norm2 = ret;
            return ret;
        }
    }
}