using System;
using System.IO;
using System.Reflection;

namespace Job.Tests
{
    internal static class TestsHelper
    {
        public static readonly string CurrentDirectory;

        static TestsHelper()
        {
            string path = Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().Location).Path);
            var currentDirectory = Path.GetDirectoryName(path) ?? Environment.ProcessPath ?? Environment.CurrentDirectory;
            CurrentDirectory = Path.Combine(currentDirectory, "Configs");
        }
    }
}
