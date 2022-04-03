using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using MediaToolkit;

namespace Hik.Client.Helpers
{
    public class VideoHelper : IVideoHelper
    {
        private readonly Engine engine = new Engine();
        private readonly string[] videoExtentions = new[] { ".mp4", ".avi" };

        public int GetDuration(string path)
        {
            if (videoExtentions.Contains(Path.GetExtension(path)))
            {
                MediaToolkit.Model.MediaFile inputFile = new MediaToolkit.Model.MediaFile { Filename = path };

                engine.GetMetadata(inputFile);

                if (inputFile is { Metadata: { } })
                {
                    return (int)inputFile.Metadata.Duration.TotalSeconds;
                }
            }

            return 0;
        }

        public async Task<string> GetThumbnailAsync(string path)
        {
            if (videoExtentions.Contains(Path.GetExtension(path)))
            {
                MediaToolkit.Model.MediaFile inputFile = new MediaToolkit.Model.MediaFile { Filename = path };
                var outputFile = new MediaToolkit.Model.MediaFile { Filename = Path.GetTempFileName() + ".jpg" };

                engine.GetThumbnail(inputFile, outputFile, new MediaToolkit.Options.ConversionOptions());

                using (Image image = Image.FromFile(outputFile.Filename))
                {
                    using (Image newImage = new Bitmap(image, 900, 506))
                    {
                        image.Dispose();
                        newImage.Save(outputFile.Filename);
                    }
                }

                byte[] imageArray = await File.ReadAllBytesAsync(outputFile.Filename);
                return Convert.ToBase64String(imageArray);
            }

            return string.Empty;
        }
    }
}
