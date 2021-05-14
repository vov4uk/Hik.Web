using System.IO;
using Hik.Client.Abstraction;
using MediaToolkit;

namespace Hik.Client.Helpers
{
    public class VideoHelper : IVideoHelper
    {
        private readonly Engine engine = new Engine();

        public int GetDuration(string path)
        {
            if (Path.GetExtension(path) == ".mp4")
            {
                MediaToolkit.Model.MediaFile inputFile = new MediaToolkit.Model.MediaFile { Filename = path };

                engine.GetMetadata(inputFile);

                if (inputFile.Metadata != null)
                {
                    return (int)inputFile.Metadata.Duration.TotalSeconds;
                }
            }

            return 0;
        }
    }
}
