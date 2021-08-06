using System;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hik.Web.Scheduler
{
    public static class QuartzTriggers
    {
        private static readonly IReadOnlyCollection<TriggerKey> TriggerKeys;

        public static async Task<IEnumerable<CronTriggerImpl>> GetCronTriggersAsync()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            var scheduler = await schedulerFactory.GetScheduler("default");
            if (scheduler == null)
            {
                throw new NullReferenceException(nameof(scheduler));
            }
            var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());

            List<CronTriggerImpl> resultList = new List<CronTriggerImpl>();
            foreach (var t in triggerKeys)
            {
                var triggerImpl = await scheduler.GetTrigger(t);
                resultList.Add(triggerImpl as CronTriggerImpl);
            }
            return resultList;
        }
    }
}
