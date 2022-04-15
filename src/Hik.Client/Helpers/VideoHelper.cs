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

        public static async Task<string> GetThumbnailAsync(string path)
        {
            if (VideoExtentions.Contains(Path.GetExtension(path)))
            {
                InputFile inputFile = new InputFile(path);

                var fullPath = Path.GetTempFileName() + ".jpg";
                var outputFile = new OutputFile(fullPath);
                await GetEngine().GetThumbnailAsync(inputFile, outputFile, CancellationToken.None);

                using (Image image = Image.FromFile(fullPath))
                {
                    using (Image newImage = new Bitmap(image, 1080, 608))
                    {
                        image.Dispose();
                        var parameters = GetCompressParameters();
                        newImage.Save(fullPath, parameters.jpgEncoder, parameters.encoderParameters);
                    }
                }

                byte[] imageArray = await File.ReadAllBytesAsync(fullPath);
                return Convert.ToBase64String(imageArray);
            }

            return string.Empty;
        }

        public async Task<int> GetDuration(string path)
        {
            if (VideoExtentions.Contains(Path.GetExtension(path)))
            {
                InputFile inputFile = new InputFile(path);

                var metadata = await GetEngine().GetMetaDataAsync(inputFile, CancellationToken.None);

                if (metadata != null)
                {
                    return (int)metadata.Duration.TotalSeconds;
                }
            }

            return 0;
        }

        private static Engine GetEngine() => new Engine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"FFMpeg\ffmpeg.exe"));

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
