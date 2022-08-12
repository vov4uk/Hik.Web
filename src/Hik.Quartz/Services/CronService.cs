using Hik.Helpers.Abstraction;
using Hik.Quartz.Contracts;
using Hik.Quartz.Contracts.Options;
using Hik.Quartz.Contracts.Xml;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
        private readonly XmlSerializer serializer = new XmlSerializer(typeof(JobSchedulingData));
        private readonly IFilesHelper filesHelper;
        public CronService(IFilesHelper filesHelper)
        {
            this.filesHelper = filesHelper;
        }

        public async Task<IReadOnlyCollection<CronDto>> GetAllCronsAsync()
        {
            IScheduler scheduler = await new StdSchedulerFactory().GetScheduler("default") ?? throw new InvalidOperationException("Unable to load default scheduler");

            IReadOnlyCollection<TriggerKey> triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());

            List<CronDto> resultList = new List<CronDto>();
            foreach (var cronTrigger in triggerKeys)
            {
                var triggerImpl = await scheduler.GetTrigger(cronTrigger) as CronTriggerImpl;

                var ctronTriggerDto = new CronDto(triggerImpl);

                resultList.Add(ctronTriggerDto);
            }
            return resultList.OrderBy(x => x.Name).ToList();
        }

        public async Task<CronDto> GetCronAsync(IConfiguration configuration, string name, string group)
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
                        return new CronDto(cron);
                    }
                }
            }
            return default(CronDto);
        }

        public async Task RestartSchedulerAsync(IConfiguration configuration)
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            var currentScheduler = await schedulerFactory.GetScheduler("default");
            await currentScheduler.Shutdown(false);

            var options = new QuartzOption(configuration);

            QuartzStartup.InitializeJobs(options.Plugin.JobInitializer.FileNames);

            schedulerFactory = new StdSchedulerFactory(options.ToProperties);
            var scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();
        }

        public async Task UpdateCronAsync(IConfiguration configuration, CronDto cron)
        {
            JobSchedulingData data = new JobSchedulingData();

            var xmlFilePath = new QuartzOption(configuration).Plugin.JobInitializer.FileNames;
            var jsonFilePath = xmlFilePath + ".json";
            var json = await filesHelper.ReadAllText(jsonFilePath);

            var dict = JsonConvert.DeserializeObject<Dictionary<string, List<CronDto>>>(json);

            string className = cron.ClassName;

            if (dict.ContainsKey(className))
            {
                var arr = dict[className];
                var original = arr.FirstOrDefault(x => x.Group == cron.Group && x.Name == cron.Name);
                if (original != null)
                {
                    arr.Remove(original);
                }
            }
            else
            {
                dict.Add(className, new List<CronDto>());
            }

            dict[className].Add(cron);

            filesHelper.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(dict));

            QuartzStartup.InitializeJobs(xmlFilePath, dict);
        }
    }
}
