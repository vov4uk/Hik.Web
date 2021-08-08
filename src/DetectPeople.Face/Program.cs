using DetectPeople.Face.Helpers;
using OpenCvSharp;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace DetectPeople.Face
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //var mat = new Mat(@"C:\Users\vkhmelovskyi\Pictures\2021-07-12.png");

            //var _detectFace = new DlibAlgorithm();

            //_detectFace.Detect(mat);
            //Bitmap bitmapImage = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
            //bitmapImage.Save(@"C:\Users\vkhmelovskyi\Pictures\result.jpg");
            //mat.Dispose();

            string img = @"C:\Users\vkhmelovskyi\Pictures\6380130322219008.jpg";
            var src = new FacialRecognitionService();
            src.Initialize();

            var res = src.Search(img);

            foreach (var result in res)
            {
                var match = result.Matches.First();
                if (match.Confidence > 0.8)
                {
                    ImageProcessingHelper.CreateImage(img, Path.Combine(Path.GetDirectoryName(match.ImagePath), $"{match.Confidence}.jpg"), result.FaceLocation);
                    Console.WriteLine($"{match.Confidence} {match.Name} {match.ImagePath}");
                }
                else // new person
                {
                    ImageProcessingHelper.CreateImage(img, Path.Combine(PathHelper.ImagesFolder(), Path.GetFileName(img)), result.FaceLocation);
                }
            }
            Console.ReadKey();
        }
    }
}
