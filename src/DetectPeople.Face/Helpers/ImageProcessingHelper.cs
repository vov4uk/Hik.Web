using FaceRecognitionDotNet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.IO;
using Image = SixLabors.ImageSharp.Image;

namespace DetectPeople.Face.Helpers
{
    public static class ImageProcessingHelper
    {
        public static string ConvertToBase64(this Stream stream)
        {
            var bytes = new byte[(int)stream.Length];

            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(bytes, 0, (int)stream.Length);

            return Convert.ToBase64String(bytes);
        }

        public static void CreateImage(string originalImagePath, string target, Location FaceLocation)
        {
            var faceRectangle = new Rectangle(
            FaceLocation.Left,
            FaceLocation.Top,
            FaceLocation.Right - FaceLocation.Left,
            FaceLocation.Bottom - FaceLocation.Top);

            using (var inStream = new FileStream(originalImagePath, FileMode.Open))
            using (var image = Image.Load(inStream))
            {
                using Image clone = image.Clone(i => i.Crop(faceRectangle));

                clone.Save(target);
            }
        }
    }
}
