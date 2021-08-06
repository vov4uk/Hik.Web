using DetectPeople.YOLOv4;
using DetectPeople.YOLOv4.DataStructures;
using Hik.DTO.Config;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NLog;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DetectPeople.Service
{
    public class PeopleDetectWorker : BackgroundService
    {
        protected readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly Scorer _scorer;

        public PeopleDetectWorker()
        {
            _scorer = new Scorer();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.Info("Service started");
            RabbitMQConfig config;
            var rootDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var configPath = Path.Combine(rootDir, "config.json");
            if (!File.Exists(configPath))
            {
                logger.Error($"\"{configPath}\" does not exist.");
                logger.Info("Use default config");
                config = new RabbitMQConfig {HostName = "localhost", QueueName = "hik" };
            }
            else
            {
                config = JsonConvert.DeserializeObject<RabbitMQConfig>(File.ReadAllText(configPath));
            }

            var rabbit = new RabbitMQHelper(config.HostName, config.QueueName);
            rabbit.Received += Rabbit_Received;
            rabbit.Consume();

            var tcs = new TaskCompletionSource<bool>();
            stoppingToken.Register(s =>
            {
                rabbit.Close();
                ((TaskCompletionSource<bool>)s).SetResult(true);
            }, tcs);
            await tcs.Task;
            rabbit.Dispose();
            logger.Info("Service stopped");
        }

        private async void Rabbit_Received(object sender, HikMessageEventArgs e)
        {
            var msg = e.Message;
            if (!File.Exists(msg.OldFilePath))
            {
                logger.Warn($"{msg.OldFilePath} not exist");
                return;
            }
            var sw = new Stopwatch();
            sw.Start();
            bool personDetected = await DetectPerson(msg.OldFilePath);
            sw.Stop();
            logger.Debug($"Done in {sw.ElapsedMilliseconds}ms.");
            if (personDetected)
            {
                File.Move(msg.OldFilePath, msg.NewFilePath, true);
            }
            else
            {
                if (msg.DeleteJunk)
                {
                    File.Delete(msg.OldFilePath);
                }
                else
                {
                    File.Move(msg.OldFilePath, msg.JunkFilePath, true);
                }
            }
        }

        private async Task<bool> DetectPerson(string photoPath)
        {
            IReadOnlyList<Result> objects = await _scorer.DetectObjectsAsync(photoPath);
            return objects.Any(IsPerson);
        }

        private bool IsPerson(Result res)
        {
            if (res.Id == 0)// person
            {
                var x1 = res.BBox[0];
                var y1 = res.BBox[1];
                var x2 = res.BBox[2];
                var y2 = res.BBox[3];
                var H = y2 - y1;
                var W = x2 - x1;

                return H > 200.0 && W > 100.0;
            }
            return false;
        }
    }
}
