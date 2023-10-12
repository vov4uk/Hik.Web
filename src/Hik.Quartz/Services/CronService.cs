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

        public async Task<IReadOnlyCollection<CronDto>> GetAllTriggersAsync()
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

        public async Task<CronDto> GetTriggerAsync(IConfiguration configuration, string name, string group)
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

        public async Task UpdateTriggerAsync(IConfiguration configuration, CronDto cron)
        {
            var triggerList = await GetTriggersList(configuration);

            string className = cron.ClassName;

            if (triggerList.ContainsKey(className))
            {
                var arr = triggerList[className];
                var original = arr.FirstOrDefault(x => x.Group == cron.Group && x.Name == cron.Name);
                if (original != null)
                {
                    arr.Remove(original);
                }
            }
            else
            {
                triggerList.Add(className, new List<CronDto>());
            }

            triggerList[className].Add(cron);

            InitializeTriggers(configuration, triggerList);
        }

        public async Task DeleteTriggerAsync(IConfiguration configuration, string group, string name, string className)
        {
            var triggerList = await GetTriggersList(configuration);

            if (triggerList.ContainsKey(className))
            {
                var arr = triggerList[className];
                var original = arr.FirstOrDefault(x => x.Group == group && x.Name == name);
                if (original != null)
                {
                    arr.Remove(original);
                }
            }

            InitializeTriggers(configuration, triggerList);
        }

        private async Task<Dictionary<string, List<CronDto>>> GetTriggersList(IConfiguration configuration)
        {
            var xmlFilePath = new QuartzOption(configuration).Plugin.JobInitializer.FileNames;
            var jsonFilePath = xmlFilePath + ".json";
            var json = await filesHelper.ReadAllText(jsonFilePath);

            Dictionary<string, List<CronDto>> cronList = JsonConvert.DeserializeObject<Dictionary<string, List<CronDto>>>(json) ?? new Dictionary<string, List<CronDto>>();

            return cronList;
        }

        private void InitializeTriggers(IConfiguration configuration, Dictionary<string, List<CronDto>> cronList)
        {
            var xmlFilePath = new QuartzOption(configuration).Plugin.JobInitializer.FileNames;
            var jsonFilePath = xmlFilePath + ".json";

            filesHelper.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(cronList));

            QuartzStartup.InitializeJobs(xmlFilePath, cronList);
        }
    }
}
