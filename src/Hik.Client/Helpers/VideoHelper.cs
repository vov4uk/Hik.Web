using System.IO;
using Hik.Client.Abstraction;
using MediaToolkit;

namespace Hik.Client.Helpers
{
    public class VideoHelper : IVideoHelper
    {
        public int GetDuration(string path)
        {
            var ext = Path.GetExtension(path);
            if (ext == ".mp4")
            {
                MediaToolkit.Model.MediaFile inputFile = new MediaToolkit.Model.MediaFile { Filename = path };

                using (Engine engine = new Engine())
                {
                    engine.GetMetadata(inputFile);
                }

                return (int)inputFile.Metadata.Duration.TotalSeconds;
            }

            return default;
        }
    }
}
