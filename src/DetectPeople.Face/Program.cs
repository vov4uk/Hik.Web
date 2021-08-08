using DetectPeople.Face.Helpers;
using DetectPeople.Face.Retina;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Face;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Mat = OpenCvSharp.Mat;

namespace DetectPeople.Face
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //string originalPath = @"C:\Users\vkhmelovskyi\Pictures\6380130322219008.jpg";
           // string originalPath = @"C:\Users\vkhmelovskyi\Pictures\2021-07-12.png";

            var faceDetection = new FaceDetection();
            faceDetection.IsSDD = true;
            var recognition = new FaceRecognition();
            recognition.Initialize();

            foreach (var originalPath in Directory.EnumerateFiles(@"C:\Floor1 - Copy", "*.*"))
            {
                var faces = faceDetection.OpenFile(originalPath);
                Console.WriteLine($"{faces.Count} Faces Detected - {originalPath}");

                foreach (var face in faces)
                {
                    Console.WriteLine(face.Label);
                    Mat myNewMat = new Mat(face.Mat, new Rect((int)face.Rect.X, (int)face.Rect.Y, (int)face.Rect.Width, (int)face.Rect.Height));

                    (global::Retina.RecFaceInfo, global::Retina.RecFaceInfo, double?) res = recognition.Search(myNewMat);
                    var result = res.Item1;

                    var match = res.Item3;
                    Console.WriteLine(match);
                    if (match > 0.7)
                    {
                        Console.WriteLine(res.Item2.Label);

                        var newPersonPath = Path.Combine(PathHelper.ImagesFolder(), res.Item2.Label, Path.GetFileName(originalPath));
                        newPersonPath = Path.ChangeExtension(newPersonPath, ".jpg");
                        Directory.CreateDirectory(Path.GetDirectoryName(newPersonPath));

                        string personFacePath = Path.ChangeExtension(newPersonPath, ".png");
                        var parts = newPersonPath.Split(new char[] { '\\', '/' });
                        var dir = parts[parts.Length - 2];

                        File.Copy(originalPath, newPersonPath, true);
                        myNewMat.SaveImage(personFacePath);
                        // File.Copy(originalPath, personFacePath, true);
                        result.FilePath = personFacePath;
                        result.Label = dir;
                        result.Face = Cv2.ImRead(personFacePath);
                        recognition.AddFace(result);
                    }
                    else // new person
                    {
                        Console.WriteLine("New person");

                        var newPersonPath = Path.Combine(PathHelper.ImagesFolder(), Guid.NewGuid().ToString(), Path.GetFileName(originalPath));
                        newPersonPath = Path.ChangeExtension(newPersonPath, ".jpg");
                        Directory.CreateDirectory(Path.GetDirectoryName(newPersonPath));

                        string personFacePath = Path.ChangeExtension(newPersonPath, ".png");
                        var parts = newPersonPath.Split(new char[] { '\\', '/' });
                        var dir = parts[parts.Length - 2];
                        //result.Face.SaveImage(personFacePath);

                        //ImageProcessingHelper.CreateImage(originalPath, personFacePath, new FaceRecognitionDotNet.Location((int)face.Rect.Left, (int)face.Rect.Top, (int)face.Rect.Right, (int)face.Rect.Bottom));

                        File.Copy(originalPath, newPersonPath, true);
                        myNewMat.SaveImage(personFacePath);
                        // File.Copy(originalPath, personFacePath, true);
                        result.FilePath = personFacePath;
                        result.Label = dir;
                        result.Face = Cv2.ImRead(personFacePath);
                        recognition.AddFace(result);
                    }
                }
            }


            Console.ReadKey();
        }



        //var mat = new Mat(@"C:\Users\vkhmelovskyi\Pictures\2021-07-12.png");

        //var _detectFace = new DlibAlgorithm();

        //_detectFace.Detect(mat);
        //Bitmap bitmapImage = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
        //bitmapImage.Save(@"C:\Users\vkhmelovskyi\Pictures\result.jpg");
        //mat.Dispose();


        //string img = @"C:\Users\vkhmelovskyi\Pictures\6380130322219008.jpg";
        //var src = new FacialRecognitionService();
        //src.Initialize();

        //var res = src.Search(img);

        //foreach (var result in res)
        //{
        //    var match = result.Matches.First();
        //    if (match.Confidence > 0.8)
        //    {
        //        ImageProcessingHelper.CreateImage(img, Path.Combine(Path.GetDirectoryName(match.ImagePath), $"{match.Confidence}.jpg"), result.FaceLocation);
        //        Console.WriteLine($"{match.Confidence} {match.Name} {match.ImagePath}");
        //    }
        //    else // new person
        //    {
        //        ImageProcessingHelper.CreateImage(img, Path.Combine(PathHelper.ImagesFolder(), Path.GetFileName(img)), result.FaceLocation);
        //    }
        //}
    }
}
