using System;
using System.IO;
using System.Reflection;

namespace Job
{
    public class Parameters
    {
        public string ClassName { get; private set; }

        public Guid ActivityId { get; set; }

        public string TriggerKey { get; set; }

        public string Group { get; set; }

        public string ConfigFilePath { get; set; }

        public string ConnectionString { get; set; }

        public bool RunAsTask { get; set; } = true;

        public override string ToString()
        {
            return $"\"{ClassName}\" \"{Group}\" \"{TriggerKey}\" \"{ActivityId}\" \"{ConfigFilePath}\" \"{ConnectionString}\" \"{RunAsTask}\"";
        }

        public Parameters(string className, string group, string description, string configFilePath, string connectionString, bool runAsTask = false)
        {
            TriggerKey = description;
            ClassName = className;
            Group = group;
            ConfigFilePath = Path.Combine(AssemblyDirectory, "Config", configFilePath);
            ConnectionString = connectionString;
            RunAsTask = runAsTask;
        }


        private Parameters()
        {
        }

        public static Parameters Parse(string[] args)
        {
            var parameters = new Parameters();
            parameters.ClassName = args[0];
            parameters.Group = args[1];
            parameters.TriggerKey = args[2];
            parameters.ActivityId = Guid.Parse(args[3]);
            parameters.ConfigFilePath = args[4];
            parameters.ConnectionString = args[5];
            parameters.RunAsTask = args[6] == "true";
            return parameters;
        }
        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().Location;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
