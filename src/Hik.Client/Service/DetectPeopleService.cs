using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.DTO.Message;
using Hik.Helpers.Abstraction;

namespace Hik.Client.Service
{
    public class DetectPeopleService : RecurrentJobBase, IDetectPeopleService
    {
        private readonly IFilesHelper filesHelper;
        private readonly IRabbitMQFactory factory;

        public DetectPeopleService(
            IDirectoryHelper directoryHelper,
            IFilesHelper filesHelper,
            IRabbitMQFactory factory)
            : base(directoryHelper)
        {
            this.filesHelper = filesHelper;
            this.factory = factory;
        }

        protected override Task<IReadOnlyCollection<MediaFileDto>> RunAsync(BaseConfig config, DateTime from, DateTime to)
        {
            var dConfig = config as DetectPeopleConfig;
            var result = new List<MediaFileDto>();
            var allFiles = this.directoryHelper.EnumerateFiles(dConfig.SourceFolder, new[] { ".jpg" });

            var mqConfig = dConfig.RabbitMQConfig;
            if (mqConfig != null)
            {
                using var rabbitMq = factory.Create(mqConfig.HostName, mqConfig.QueueName, mqConfig.RoutingKey);
                foreach (var filePath in allFiles)
                {
                    string fileName = filesHelper.GetFileName(filePath);

                    string newFilePath = filesHelper.CombinePath(dConfig.DestinationFolder, fileName);
                    string junkFilePath = filesHelper.CombinePath(dConfig.JunkFolder, fileName);

                    rabbitMq.Sent(new DetectPeopleMessage
                    {
                        UniqueId = Guid.NewGuid().ToString(),
                        OldFilePath = filePath,
                        NewFilePath = newFilePath,
                        NewFileName = fileName,
                        JunkFilePath = junkFilePath,
                        DeleteJunk = false,
                    });

                    result.Add(new MediaFileDto
                    {
                        Name = fileName,
                        Path = filePath,
                    });
                }
            }

            return Task.FromResult(result as IReadOnlyCollection<MediaFileDto>);
        }
    }
}
