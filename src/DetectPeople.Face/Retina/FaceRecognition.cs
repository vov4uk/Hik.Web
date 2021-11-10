using DetectPeople.Face.Helpers;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DetectPeople.Face.Retina
{
    //https://github.com/fel88/Retina
    public class FaceRecognition
    {
        private readonly FaceNetRecognition faceNetRec = new FaceNetRecognition();

        public FaceRecognition()
        {
        }

        public void AddFace(FaceRecognitionResult face)
        {
            if (face != null)
            {
                var emb = Path.ChangeExtension(face.FilePath, "emb");
                if (face.Encoding != null)
                {
                    File.WriteAllBytes(emb, GetBytes(face.Encoding));
                }
                else if (face.Face != null)
                {
                    float[] Embedding = FaceNetRecognition.EncodeImg(face.Face);
                    File.WriteAllBytes(emb, GetBytes(Embedding));
                }

                FaceNetRecognition.Faces.Add(face);
            }
            else
            {
                Console.WriteLine($"Face is null {face.FilePath}");
            }
        }

        public void EncodeDataSet()
        {
            foreach (var item in FaceNetRecognition.Faces)
            {
                if (!File.Exists(item.FilePath))
                {
                    item.Face.SaveImage(item.FilePath);
                }

                var emb = Path.ChangeExtension(item.FilePath, "emb");
                if (!File.Exists(emb))
                {
                    File.WriteAllBytes(emb, GetBytes(item.Encoding));
                }
            }
        }

        public void Initialize()
        {
            Console.WriteLine();
            Console.WriteLine("Initialize DB");
            Console.WriteLine();
            DeserializeFaces(PathHelper.ImagesFolder());
        }

        public FaceNetRecognitionResult Recognize(Mat inputImg)
        {
            var faces = FaceNetRecognition.Faces.Select(x => x.Label.ToLowerInvariant()).Distinct();
            var imgFolder = PathHelper.ImagesFolder();
            var directories = Directory.EnumerateDirectories(imgFolder, "*.*", SearchOption.TopDirectoryOnly)
                .Select(x => x.Replace(imgFolder, string.Empty).Replace("\\",string.Empty).ToLowerInvariant());

            if (!faces.SequenceEqual(directories))
            {
                Initialize();
            }

            return faceNetRec.Recognize(inputImg);
        }

        private void DeserializeFaces(string folder)
        {
            if (!Directory.Exists(folder)) return;

            IEnumerable<string> pngs = Directory.EnumerateFiles(folder, "*.png", SearchOption.AllDirectories);
            FaceNetRecognition.Faces.Clear();
            foreach (var file in pngs)
            {
                var parts = file.Split(new char[] { '\\', '/' });
                var dir = parts[^2];

                var emb = Path.ChangeExtension(file, "emb");
                FaceRecognitionResult f = new FaceRecognitionResult
                {
                    Label = dir,
                    FilePath = file,
                    Face = Cv2.ImRead(file)
                };

                if (File.Exists(emb))
                {
                    f.Encoding = LoadArray(emb);
                }
                else
                {
                    f.Encoding = FaceNetRecognition.EncodeImg(f.Face);
                    File.WriteAllBytes(emb, GetBytes(f.Encoding));
                }

                FaceNetRecognition.Faces.Add(f);
            }
        }

        private byte[] GetBytes(float[] array)
        {
            List<byte> dd = new List<byte>();
            foreach (var item in array)
            {
                dd.AddRange(BitConverter.GetBytes(item));
            }
            return dd.ToArray();
        }

        private float[] LoadArray(string fullName)
        {
            List<float> ret = new List<float>();
            var bb = File.ReadAllBytes(fullName);
            for (int i = 0; i < bb.Length; i += 4)
            {
                ret.Add(BitConverter.ToSingle(bb, i));
            }
            return ret.ToArray();
        }
    }
}