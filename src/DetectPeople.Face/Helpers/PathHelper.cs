using System;
using System.IO;
using System.Reflection;

namespace DetectPeople.Face.Helpers
{
    public static class PathHelper
    {
        public static string BinPath()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        public const string ResultFolder = "results";

        public static string ModelsFolder()
        {
            var modelsFolder = Environment.GetEnvironmentVariable("KURDCELEBS_MODELS_DIR")
                ?? Constants.ModelsFolder;

            return Path.Combine(BinPath(), modelsFolder);
        }

        public static string ImagesFolder()
        {
            var imagesFolder = Environment.GetEnvironmentVariable("KURDCELEBS_IMAGES_DIR")
                ?? Constants.ImagesFolder;

            return Path.Combine(BinPath(), imagesFolder);
        }
    }
}
