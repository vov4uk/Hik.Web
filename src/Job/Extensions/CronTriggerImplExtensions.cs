using Quartz.Impl.Triggers;

namespace Job.Extensions
{
    public static class CronTriggerImplExtensions
    {
        private const string Job = "Job";
        private const string Config = "ConfigPath";
        private const string RunAsTask = "RunAsTask";
        public static string GetJobClass(this CronTriggerImpl item)
        {
            return GetValue(item, Job);
        }

        public static string GetConfig(this CronTriggerImpl item)
        {
            return GetValue(item, Config);
        }

        public static string GetRunAsTask(this CronTriggerImpl item)
        {
            return GetValue(item, RunAsTask);
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
