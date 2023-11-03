using System;
using System.Diagnostics.CodeAnalysis;

namespace Job
{
    [ExcludeFromCodeCoverage]
    public class Parameters
    {
        public Guid ActivityId { get; set; }

        public string TriggerKey { get; set; }

        public string Group { get; set; }

        public string ConfigPath { get; set; }

        public string Environment { get; set; }

        public override string ToString()
            => $"\"{Group}\" \"{TriggerKey}\" \"{ActivityId}\" \"{ConfigPath}\" \"{Environment}\"";

        public Parameters(string group, string triggerKey, string configPath, string environment)
        {
            TriggerKey = triggerKey;
            Group = group;
            ConfigPath = configPath;
            Environment = environment;
        }

        private Parameters()
        {
        }

        public static Parameters Parse(string[] args)
            => new Parameters
            {
                Group = args[0],
                TriggerKey = args[1],
                ActivityId = Guid.Parse(args[2]),
                ConfigPath = args[3],
                Environment = args[4]
            };
    }
}