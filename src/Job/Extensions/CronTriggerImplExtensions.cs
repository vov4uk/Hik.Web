using Quartz.Impl.Triggers;

namespace Job.Extensions
{
    public static class CronTriggerImplExtensions
    {
        private const string job = "Job";
        private const string config = "ConfigPath";
        private const string runAsTask = "RunAsTask";
        public static string GetJobClass(this CronTriggerImpl item)
        {
            return GetValue(item, job);
        }

        public static string GetConfig(this CronTriggerImpl item)
        {
            return GetValue(item, config);
        }
        
        public static string GetRunAsTask(this CronTriggerImpl item)
        {
            return GetValue(item, runAsTask);
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
