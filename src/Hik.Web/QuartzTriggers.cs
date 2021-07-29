using System;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hik.Web
{
    public static class QuartzTriggers
    {
        private static readonly IReadOnlyCollection<TriggerKey> TriggerKeys;
        private static readonly IScheduler Scheduler;
        static QuartzTriggers()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            Scheduler = schedulerFactory.GetScheduler("default").GetAwaiter().GetResult();
            if (Scheduler == null)
            {
                throw new NullReferenceException(nameof(Scheduler));
            }
            TriggerKeys = Scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup()).GetAwaiter().GetResult();
        }

        public static async Task<IEnumerable<CronTriggerImpl>> GetCronTriggersAsync()
        {
            List<CronTriggerImpl> resultList = new List<CronTriggerImpl>();
            foreach (var t in TriggerKeys)
            {
                var triggerImpl = await Scheduler.GetTrigger(t);
                resultList.Add(triggerImpl as CronTriggerImpl);
            }
            return resultList;
        }
    }
}
