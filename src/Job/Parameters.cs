using System;
using System.Diagnostics.CodeAnalysis;

namespace Job
{
    [ExcludeFromCodeCoverage]
    public class Parameters
    {
        public Guid ActivityId { get; private set; }

        public string TriggerKey { get; set; }

        public string Group { get; set; }

        public string Environment { get; set; }

        public override string ToString()
            => $"\"{Group}\" \"{TriggerKey}\" \"{ActivityId}\" \"{Environment}\"";

        public Parameters(string group, string triggerKey, string environment)
        {
            TriggerKey = triggerKey;
            Group = group;
            Environment = environment;
            ActivityId = Guid.NewGuid();
        }

        private Parameters()
        {
        }

        public static Parameters Parse(string[] args)
            => new()
            {
                Group = args[0],
                TriggerKey = args[1],
                ActivityId = Guid.Parse(args[2]),
                Environment = args[3]
            };
    }
}