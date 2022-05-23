using Hik.Helpers.Abstraction;
using Hik.Quartz.Contracts;
using Hik.Quartz.Contracts.Options;
using Hik.Quartz.Contracts.Xml;
using Microsoft.Extensions.Configuration;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Hik.Quartz.Services
{
    public class CronService : ICronService
    {
        XmlSerializer serializer = new XmlSerializer(typeof(JobSchedulingData));
        private readonly IFilesHelper filesHelper;
        public CronService(IFilesHelper filesHelper)
        {
            this.filesHelper = filesHelper;
        }

        public async Task<IReadOnlyCollection<CronDTO>> GetAllCronsAsync()
        {
            IScheduler scheduler = await new StdSchedulerFactory().GetScheduler("default") ?? throw new InvalidOperationException("Unable to load default scheduler");

            IReadOnlyCollection<TriggerKey> triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());

            List<CronDTO> resultList = new List<CronDTO>();
            foreach (var cronTrigger in triggerKeys)
            {
                var triggerImpl = await scheduler.GetTrigger(cronTrigger) as CronTriggerImpl;

                var ctronTriggerDto = new CronDTO(triggerImpl);

                resultList.Add(ctronTriggerDto);
            }
            return resultList.OrderBy(x => x.Name).ToList();
        }

        public async Task<CronDTO> GetCronAsync(IConfiguration configuration, string name, string group)
        {
            var options = new QuartzOption(configuration);
            var xmlFilePath = options.Plugin.JobInitializer.FileNames;
            var xml = await filesHelper.ReadAllText(xmlFilePath);

            using (StringReader reader = new StringReader(xml))
            {
                var data = (JobSchedulingData)serializer.Deserialize(reader);

                if (data.Schedule.Trigger.Any())
                {
                    Cron cron = data.Schedule.Trigger.Select(x => x.Cron).FirstOrDefault(x => x.Group == group && x.Name == name);
                    if (cron != null)
                    {
                        return new CronDTO(cron);
                    }
                }
            }
            return default(CronDTO);
        }

        public async Task RestartSchedulerAsync(IConfiguration configuration)
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            var currentScheduler = await schedulerFactory.GetScheduler("default");
            await currentScheduler.Shutdown(false);

            schedulerFactory = new StdSchedulerFactory(new QuartzOption(configuration).ToProperties());
            var scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();
        }

        public async Task UpdateCronAsync(IConfiguration configuration, CronDTO cron)
        {
            JobSchedulingData data;
            var options = new QuartzOption(configuration);
            var xmlFilePath = options.Plugin.JobInitializer.FileNames;
            var xml = await filesHelper.ReadAllText(xmlFilePath);

            using (StringReader reader = new StringReader(xml))
            {
                data = (JobSchedulingData)serializer.Deserialize(reader);
            }

            var original = data.Schedule.Trigger.FirstOrDefault(x => x.Cron.Group == cron.Group && x.Cron.Name == cron.Name);
            if (original != null)
            {
                data.Schedule.Trigger.Remove(original);
            }

            data.Schedule.Trigger.Add(new Trigger { Cron = cron.ToCron() });

            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, data);
                filesHelper.WriteAllText(xmlFilePath, writer.ToString());
            }
        }
    }
}
