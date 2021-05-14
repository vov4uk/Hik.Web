using System;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hik.Web
{
    public static class QuartzTriggers
    {
        private static IList<CronTriggerImpl> _quartzTriggers;
        public static IList<CronTriggerImpl> Instance
        {
            get
            {
                return _quartzTriggers ??= BuildModelAsync().GetAwaiter().GetResult();
            }
        }

        private static async Task<IList<CronTriggerImpl>> BuildModelAsync()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            IScheduler scheduler = await schedulerFactory.GetScheduler("default");
            if (scheduler == null)
            {
                throw new NullReferenceException(nameof(scheduler));
            }
            IReadOnlyCollection<TriggerKey> triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
            var triggers = triggerKeys?.Select(async t => await scheduler.GetTrigger(t)).ToArray();
            
            if (triggers == null)
            {
                throw new NullReferenceException(nameof(triggers));
            }
            Task.WaitAll(triggers);
            return triggers.Select(x => x.Result as CronTriggerImpl).ToList();
        }
    }
}
