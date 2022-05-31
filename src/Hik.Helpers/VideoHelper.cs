using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.NET;
using Hik.Helpers.Abstraction;

namespace Hik.Helpers
{
    public class VideoHelper : IVideoHelper
    {
        private static readonly string[] VideoExtentions = new[] { ".mp4", ".avi" };

        public async Task<string> GetThumbnailStringAsync(string path)
        {
            if (VideoExtentions.Contains(Path.GetExtension(path)))
            {
                InputFile inputFile = new InputFile(path);

                var fullPath = Path.GetRandomFileName() + ".jpg";
                var outputFile = new OutputFile(fullPath);
                await GetEngine().GetThumbnailAsync(inputFile, outputFile, CancellationToken.None);

                Image image = Image.FromFile(fullPath);
                using (Image newImage = new Bitmap(image, 1080, 608))
                {
                    image.Dispose();
                    var parameters = GetCompressParameters();
                    newImage.Save(fullPath, parameters.jpgEncoder, parameters.encoderParameters);
                }

                byte[] imageArray = await File.ReadAllBytesAsync(fullPath);
                File.Delete(fullPath);
                return $"data:image/jpg;base64,{Convert.ToBase64String(imageArray)}";
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

        private static Engine GetEngine() => new Engine(Path.Combine(Environment.CurrentDirectory, @"FFMpeg\ffmpeg.exe"));

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
    }
}
