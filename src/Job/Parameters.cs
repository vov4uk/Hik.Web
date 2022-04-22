using System;
using System.IO;

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
            => $"\"{ClassName}\" \"{Group}\" \"{TriggerKey}\" \"{ActivityId}\" \"{ConfigFilePath}\" \"{ConnectionString}\" \"{RunAsTask}\"";

        public Parameters(string className, string group, string description, string configFilePath, string connectionString, bool runAsTask = false)
        {
            TriggerKey = description;
            ClassName = className;
            Group = group;
            ConfigFilePath = Path.Combine(Environment.CurrentDirectory, "Config", configFilePath);
            ConnectionString = connectionString;
            RunAsTask = runAsTask;
        }

        private Parameters()
        {
        }

        public static Parameters Parse(string[] args)
            => new Parameters
            {
                ClassName = args[0],
                Group = args[1],
                TriggerKey = args[2],
                ActivityId = Guid.Parse(args[3]),
                ConfigFilePath = args[4],
                ConnectionString = args[5],
                RunAsTask = args[6] == "true"
            };
    }
}