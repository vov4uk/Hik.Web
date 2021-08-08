using DetectPeople.Face.Helpers;
using OpenCvSharp;
using Retina;
using System;
using System.Collections.Generic;
using System.IO;

namespace DetectPeople.Face.Retina
{
    public class FaceRecognition
    {
        FaceNet net = new FaceNet();
        public FaceRecognition()
        {
        }

        public void Initialize()
        {
            DeserializeFaces(PathHelper.ImagesFolder());
        }

        private void DeserializeFaces(string folder)
        {
            if (!Directory.Exists(folder)) return;

            IEnumerable<string> pngs = Directory.EnumerateFiles(folder, "*.png", SearchOption.AllDirectories);
            FaceNet.Faces.Clear();
            foreach (var file in pngs)
            {
                var parts = file.Split(new char[] { '\\', '/' });
                var dir = parts[parts.Length - 2];

                var emb = Path.ChangeExtension(file, "emb");
                RecFaceInfo f = new RecFaceInfo();
                f.Label = dir;
                f.FilePath = file;
                f.Face = OpenCvSharp.Cv2.ImRead(file);

                if (File.Exists(emb))
                {
                    f.Embedding = loadArray(emb);
                }
                else
                {
                    f.Embedding = FaceNet.Inference(f.Face);
                    File.WriteAllBytes(emb, getBytes(f.Embedding));
                }

                FaceNet.Faces.Add(f);
            }
        }

        public void AddFace(RecFaceInfo face)
        {
            if (face != null)
            {
                var emb = Path.ChangeExtension(face.FilePath, "emb");
                if (face.Embedding != null)
                {
                    File.WriteAllBytes(emb, getBytes(face.Embedding));
                }
                else if (face.Face != null)
                {
                    float[] Embedding = FaceNet.Inference(face.Face);
                    File.WriteAllBytes(emb, getBytes(Embedding));
                }

                FaceNet.Faces.Add(face);
            }
            else
            {
                Console.WriteLine($"Face is null {face.FilePath}");
            }
        }

        private float[] loadArray(string fullName)
        {
            List<float> ret = new List<float>();
            var bb = File.ReadAllBytes(fullName);
            for (int i = 0; i < bb.Length; i += 4)
            {
                ret.Add(BitConverter.ToSingle(bb, i));
            }
            return ret.ToArray();
        }

        public (RecFaceInfo, RecFaceInfo, double?) Search(string fileName)
        {
            var mat = OpenCvSharp.Cv2.ImRead(fileName);
            var best = net.Recognize(mat);
            return best;
        }

        public (RecFaceInfo, RecFaceInfo, double?) Search(Mat mat)
        {
            var best = net.Recognize(mat);
            return best;
        }

        public void EncodeDataSet()
        {
            foreach (var item in FaceNet.Faces)
            {
                if (!File.Exists(item.FilePath))
                {
                    item.Face.SaveImage(item.FilePath);
                }

                var emb = Path.ChangeExtension(item.FilePath, "emb");
                if (!File.Exists(emb))
                {
                    File.WriteAllBytes(emb, getBytes(item.Embedding));
                }
            }
        }
        byte[] getBytes(float[] array)
        {
            List<byte> dd = new List<byte>();
            foreach (var item in array)
            {
                dd.AddRange(BitConverter.GetBytes(item));
            }
            return dd.ToArray();
        }
    }
}
