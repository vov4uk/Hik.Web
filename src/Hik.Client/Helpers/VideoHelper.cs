using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.NET;
using Hik.Client.Abstraction;

namespace Hik.Client.Helpers
{
    public class VideoHelper : IVideoHelper
    {
        private static readonly string[] VideoExtentions = new[] { ".mp4", ".avi" };
        private static readonly Engine Engine = new Engine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"FFMpeg\ffmpeg.exe"));

        public static async Task<string> GetThumbnailAsync(string path)
        {
            if (VideoExtentions.Contains(Path.GetExtension(path)))
            {
                InputFile inputFile = new InputFile(path);

                var options = new ConversionOptions { Seek = TimeSpan.FromSeconds(1) };

                var outputFile = new OutputFile(Path.GetTempFileName() + ".jpg");

                await Engine.GetThumbnailAsync(inputFile, outputFile, options, CancellationToken.None);

                var fullPath = outputFile.FileInfo.FullName;

                using (Image image = Image.FromFile(fullPath))
                {
                    using (Image newImage = new Bitmap(image, 1080, 608))
                    {
                        image.Dispose();
                        newImage.Save(fullPath);
                    }
                }

                var commpresed = Path.GetTempFileName() + ".jpg";
                CompressImage(fullPath, commpresed);

                byte[] imageArray = await File.ReadAllBytesAsync(commpresed);
                return Convert.ToBase64String(imageArray);
            }

            return string.Empty;
        }

        public async Task<int> GetDuration(string path)
        {
            if (VideoExtentions.Contains(Path.GetExtension(path)))
            {
                InputFile inputFile = new InputFile(path);

                var metadata = await Engine.GetMetaDataAsync(inputFile, CancellationToken.None);

                if (metadata != null)
                {
                    return (int)metadata.Duration.TotalSeconds;
                }
            }

            return 0;
        }

        private static void CompressImage(string source, string destination)
        {
            using (Bitmap bitmap = new Bitmap(source))
            {
                var parameters = GetCompressParameters();
                DeleteFile(destination);
                bitmap.Save(destination, parameters.jpgEncoder, parameters.encoderParameters);
            }
        }

        private static (ImageCodecInfo jpgEncoder, EncoderParameters encoderParameters) GetCompressParameters()
        {
            var jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 25L);

            return (jpgEncoder, encoderParameters);
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            return codecs.FirstOrDefault(c => c.FormatID == format.Guid);
        }

        private static void DeleteFile(string filepath)
        {
            try
            {
                File.Delete(filepath);
            }
            catch (Exception)
            {
            }
        }
    }
}
