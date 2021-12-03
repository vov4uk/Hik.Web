using Quartz.Impl.Triggers;

namespace Job.Extensions
{
    public static class CronTriggerImplExtensions
    {
        public const string Job = "Job";
        public const string Config = "ConfigPath";
        public const string RunAsTask = "RunAsTask";
        public static string GetJobClass(this CronTriggerImpl item)
        {
            return GetValue(item, Job);
        }

        public static string GetConfig(this CronTriggerImpl item)
        {
            return GetValue(item, Config);
        }

        public static bool GetRunAsTask(this CronTriggerImpl item)
        {
            return bool.Parse(GetValue(item, RunAsTask));
        }

        private static string GetValue(CronTriggerImpl item, string key)
        {
            if (item.JobDataMap.ContainsKey(key))
            {
                return item.JobDataMap[key].ToString();
            }
            return string.Empty;
        }
    }
}
