using System.IO;
using System.Linq;
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
    }
}
