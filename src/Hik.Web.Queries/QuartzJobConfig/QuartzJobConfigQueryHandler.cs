using Hik.Helpers.Abstraction;
using Hik.Quartz.Contracts;
using Hik.Quartz.Services;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Hik.Web.Queries.QuartzJobConfig
{
    public class QuartzJobConfigQueryHandler : QueryHandler<QuartzJobConfigQuery>
    {
        private readonly IFilesHelper filesHelper;
        private readonly ICronService cronHelper;
        private readonly IConfiguration configuration;

        public QuartzJobConfigQueryHandler(IConfiguration configuration, IFilesHelper filesHelper, ICronService cronService)
        {
            this.configuration = configuration;
            this.filesHelper = filesHelper;
            this.cronHelper = cronService;
        }

        protected override async Task<IHandlerResult> HandleAsync(QuartzJobConfigQuery request, CancellationToken cancellationToken)
        {
            var cron = await cronHelper.GetCronAsync(configuration, request.Name, request.Group);
            if (cron != null)
            {
                var configDto = new CronConfigDto(cron);
                var path = configDto.GetConfigPath();
                if (filesHelper.FileExists(path))
                {
                    var json = await filesHelper.ReadAllText(path);
                    configDto.Json = PrettyJson(json);
                }
                else
                {
                    filesHelper.WriteAllText(path, string.Empty);
                }

                return new QuartzJobConfigDto { Config = configDto };
            }

            return default(QuartzJobConfigDto);
        }

        private static string PrettyJson(string unPrettyJson)
        {
            if (string.IsNullOrEmpty(unPrettyJson))
                return string.Empty;
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(unPrettyJson);

                return JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions()
                {
                    WriteIndented = true
                });
            }
            catch (JsonException)
            {
                //OK
            }

            return unPrettyJson;
        }
    }
}
