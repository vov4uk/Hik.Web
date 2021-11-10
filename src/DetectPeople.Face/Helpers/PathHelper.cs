using System;
using System.IO;
using System.Reflection;

namespace DetectPeople.Face.Helpers
{
    public static class PathHelper
    {
        public static string BinPath => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        public static string ImagesFolder()
        {
            var imagesFolder = Environment.GetEnvironmentVariable("KURDCELEBS_IMAGES_DIR")
                ?? Constants.ImagesFolder;

            return Path.Combine(BinPath, imagesFolder);
        }
    }
}
