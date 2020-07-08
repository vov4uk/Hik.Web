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

        public string ConfigFilePath { get; set; }

        public string ConnectionString { get; set; }

        public override string ToString()
        {
            return $"\"{ClassName}\" \"{TriggerKey}\" \"{ActivityId}\" \"{ConfigFilePath}\" \"{ConnectionString}\"";
        }

        public Parameters(string className, string description, string configFilePath, string connectionString)
        {
            TriggerKey = description;
            ClassName = className;
            ConfigFilePath = Path.Combine(AssemblyDirectory, configFilePath);
            ConnectionString = connectionString;
        }


        private Parameters()
        {
        }

        public static Parameters Parse(string[] args)
        {
            var parameters = new Parameters();
            parameters.ClassName = args[0];
            parameters.TriggerKey = args[1];
            parameters.ActivityId = Guid.Parse(args[2]);
            parameters.ConfigFilePath = args[3];
            parameters.ConnectionString = args[4];
            return parameters;
        }
        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
