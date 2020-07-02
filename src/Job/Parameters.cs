using System;

namespace Job
{
    public class Parameters
    {
        public readonly string Operator = "Scheduler";

        public int JobEventId { get; private set; }

        public string ClassName { get; private set; }

        public Guid ActivityId { get; set; }

        public string Description { get; set; }

        public string ConfigFilePath { get; set; }

        public override string ToString()
        {
            return $"\"{SingletonKey}\" \"{ActivityId}\" \"{ConfigFilePath}\"";
        }

        public Parameters(string className, string description, string configFilePath)
        {
            Description = description;
            ClassName = className;
            ConfigFilePath = configFilePath;
        }


        private Parameters()
        {
        }

        public string SingletonKey
        {
            get { return ClassName; }
        }

        public static Parameters Parse(string[] args)
        {
            var parameters = new Parameters();
            parameters.ClassName = args[0];
            parameters.ActivityId = Guid.Parse(args[1]);
            parameters.ConfigFilePath = args[2];
            return parameters;
        }
    }
}
