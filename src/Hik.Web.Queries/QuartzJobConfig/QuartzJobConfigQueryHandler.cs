using Hik.Helpers.Abstraction;
using Hik.Quartz.Contracts;
using Hik.Quartz.Contracts.Options;
using Hik.Quartz.Contracts.Xml;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Xml.Serialization;

namespace Hik.Web.Queries.QuartzJobConfig
{
    public class QuartzJobConfigQueryHandler : QueryHandler<QuartzJobConfigQuery>
    {
        private readonly IFilesHelper filesHelper;
        private readonly IConfiguration configuration;

        public QuartzJobConfigQueryHandler(IConfiguration configuration, IFilesHelper filesHelper)
        {
            this.configuration = configuration;
            this.filesHelper = filesHelper;
        }

        protected override async Task<IHandlerResult> HandleAsync(QuartzJobConfigQuery request, CancellationToken cancellationToken)
        {
            var options = new QuartzOption(configuration);
            var xmlFilePath = options.Plugin.JobInitializer.FileNames;
            var xml = await filesHelper.ReadAllText(xmlFilePath);

            XmlSerializer serializer = new XmlSerializer(typeof(JobSchedulingData));
            using (StringReader reader = new StringReader(xml))
            {
                var data = (JobSchedulingData)serializer.Deserialize(reader);

                if (data.Schedule.Trigger.Any())
                {
                    var cron = data.Schedule.Trigger.Select(x => x.Cron).FirstOrDefault(x => x.Group == request.Group && x.Name == request.Name);
                    if (cron != null)
                    {
                        var ConfigDTO = new CronConfigDTO(cron);
                        if (filesHelper.FileExists(ConfigDTO.GetConfigPath()))
                        {
                            ConfigDTO.Json = PrettyJson(await filesHelper.ReadAllText(ConfigDTO.GetConfigPath()));
                        }
                        else
                        {
                            filesHelper.WriteAllText(ConfigDTO.Path, string.Empty);
                        }

                        return new QuartzJobConfigDto { ConfigDTO = ConfigDTO };
                    }
                }
            }

            return default(QuartzJobConfigDto);
        }

        private static string PrettyJson(string unPrettyJson)
        {
            if (string.IsNullOrEmpty(unPrettyJson))
                return string.Empty;

            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            var jsonElement = JsonSerializer.Deserialize<JsonElement>(unPrettyJson);

            return JsonSerializer.Serialize(jsonElement, options);
        }
    }
}
