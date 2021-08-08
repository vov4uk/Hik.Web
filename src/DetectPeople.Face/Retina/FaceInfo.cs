using OpenCvSharp;
using System;

namespace Retina
{
    public class RecFaceInfo
    {
        public Mat Face;
        public string Label;
        public float[] Embedding;
        public string FilePath { get; set; }
        double? _norm2;

        public double Norm2()
        {
            if (_norm2 != null) return _norm2.Value;
           
            double ret = 0;
            foreach (var item in Embedding)
            {
                ret += item * item;
            }
            ret = Math.Sqrt(ret);
            _norm2 = ret;
            return ret;
        }
        public double CosDist(RecFaceInfo f)
        {
            var n1 = Norm2();
            var n2 = f.Norm2();
            double dot = 0;
            for (int i = 0; i < Embedding.Length; i++)
            {
                dot += Embedding[i] * f.Embedding[i];
            }
            return dot / n1 / n2;
        }
    }
}


