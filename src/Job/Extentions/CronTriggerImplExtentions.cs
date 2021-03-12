using Quartz.Impl.Triggers;

namespace Job.Extentions
{
    public static class CronTriggerImplExtentions
    {
        private const string job = "Job";
        private const string config = "ConfigPath";
        public static string GetJobClass(this CronTriggerImpl item)
        {
            if (item.JobDataMap.ContainsKey(job))
            {
                return item.JobDataMap[job].ToString();
            }
            return string.Empty;
        }

        public static string GetConfig(this CronTriggerImpl item)
        {
            if (item.JobDataMap.ContainsKey(config))
            {
                return item.JobDataMap[config].ToString();
            }
            return string.Empty;
        }
    }
}
