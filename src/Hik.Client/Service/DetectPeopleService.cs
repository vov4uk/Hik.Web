using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Abstraction.Services;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.DTO.Message;
using Hik.Helpers.Abstraction;
using Microsoft.Extensions.Logging;

namespace Hik.Client.Service
{
    public class DetectPeopleService : RecurrentJobBase, IDetectPeopleService
    {
        private readonly IFilesHelper filesHelper;
        private readonly IRabbitMQFactory factory;

        public DetectPeopleService(
            IDirectoryHelper directoryHelper,
            IFilesHelper filesHelper,
            IRabbitMQFactory factory,
            ILogger logger)
            : base(directoryHelper, logger)
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
            if (mqConfig != null && allFiles.Any())
            {
                using var rabbitMq = factory.Create(mqConfig.HostName, mqConfig.QueueName, mqConfig.RoutingKey);
                foreach (var filePath in allFiles)
                {
                    string fileName = filesHelper.GetFileName(filePath);

                    string newFilePath = filesHelper.CombinePath(dConfig.DestinationFolder, fileName);
                    string junkFilePath = GetPathSafety(fileName, filesHelper.CombinePath(dConfig.JunkFolder, DateTime.Now.ToPhotoDirectoryNameString()));

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
                        Date = DateTime.Now,
                    });
                }
            }

            return Task.FromResult(result as IReadOnlyCollection<MediaFileDto>);
        }

        private string GetPathSafety(string file, string directory)
        {
            directoryHelper.CreateDirIfNotExist(directory);
            return filesHelper.CombinePath(directory, file);
        }
    }
}
