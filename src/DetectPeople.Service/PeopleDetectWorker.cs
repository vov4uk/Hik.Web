using DetectPeople.Face;
using DetectPeople.Face.Helpers;
using DetectPeople.YOLOv4;
using DetectPeople.YOLOv4.DataStructures;
using FaceRecognitionDotNet;
using Hik.DTO.Config;
using Hik.DTO.Message;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NLog;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DetectPeople.Service
{
    public class PeopleDetectWorker : BackgroundService
    {
        protected readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly Scorer _scorer;
        private readonly FacialRecognitionService service;

        public PeopleDetectWorker()
        {
            _scorer = new Scorer();
            service = new FacialRecognitionService();
            service.Initialize();
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.Info("PeopleDetectWorker started");
            RabbitMQConfig config;
            var rootDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var configPath = Path.Combine(rootDir, "config.json");
            if (!File.Exists(configPath))
            {
                logger.Error($"\"{configPath}\" does not exist.");
                logger.Info("Use default config");
                config = new RabbitMQConfig {HostName = "localhost", QueueName = "hik" , RoutingKey = "hik"};
            }
            else
            {
                config = JsonConvert.DeserializeObject<RabbitMQConfig>(File.ReadAllText(configPath));
            }

            RabbitMQHelper hikReceiver = new RabbitMQHelper(config.HostName, config.QueueName, config.RoutingKey);
            hikReceiver.Received += Rabbit_Received;
            hikReceiver.Consume();

            var tcs = new TaskCompletionSource<bool>();
            stoppingToken.Register(s =>
            {
                hikReceiver.Close();
                ((TaskCompletionSource<bool>)s).SetResult(true);
            }, tcs);
            await tcs.Task;
            hikReceiver.Received -= Rabbit_Received;
            hikReceiver.Dispose();
            logger.Info("Service stopped");
        }

        private async void Rabbit_Received(object sender, BasicDeliverEventArgs ea)
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            DetectPeopleMessage msg = JsonConvert.DeserializeObject<DetectPeopleMessage>(body);
            logger.Debug("[x] Received {0}", body);

            if (!File.Exists(msg.OldFilePath))
            {
                logger.Warn($"{msg.OldFilePath} not exist");
                return;
            }
            var sw = new Stopwatch();
            sw.Start();
            IReadOnlyList<ObjectDetectResult> objects = await _scorer.DetectObjectsAsync(msg.OldFilePath);
            sw.Stop();
            logger.Debug($"Done in {sw.ElapsedMilliseconds}ms.");

            if (objects.Any(IsPerson))
            {

                foreach (var result in objects.Where(IsPerson))
                {
                    DetectFaces(msg.OldFilePath, msg.NewFileName, result.BBox.Select(x => (int)x).ToArray());
                }

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


        private bool IsPerson(ObjectDetectResult res)
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

        private void DetectFaces(string filePath, string fileName, int[] BBox)
        {
            try
            {
                var tempImg = Path.GetTempFileName() + ".jpeg";
                ImageProcessingHelper.CreateImage(filePath, tempImg, new Location(BBox[0], BBox[1], BBox[2], BBox[3]));
                File.Copy(tempImg, $@"c:\tmp\{fileName}", true);
                var res = service.Search(tempImg);

                foreach (SearchResult result in res)
                {
                    var match = result.Matches.First();
                    if (match.Confidence > 0.65)
                    {
                        var personPath = Path.Combine(Path.GetDirectoryName(match.ImagePath), Path.GetFileName(fileName));
                        SavePerson(tempImg, personPath, result);

                        logger.Info($" {match.Name} : {match.Confidence} {match.ImagePath}");
                    }
                    else // new person
                    {
                        logger.Info($"new person {match.Confidence}");
                        var newPersonPath = Path.Combine(PathHelper.ImagesFolder(), Guid.NewGuid().ToString(), Path.GetFileName(fileName));
                        Directory.CreateDirectory(Path.GetDirectoryName(newPersonPath));

                        SavePerson(tempImg, newPersonPath, result);
                    }
                }

                File.Delete(tempImg);
            }
            catch (NoFaceFoundException)
            {
                logger.Warn("No face found");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private void SavePerson(string originalPath, string personPath, SearchResult result)
        {
            string personFacePath = Path.ChangeExtension(personPath,".png");
            var parts = personPath.Split(new char[] { '\\', '/' });
            var dir = parts[parts.Length - 2];
            ImageProcessingHelper.CreateImage(originalPath, personFacePath, result.FaceLocation);
            File.Move(originalPath, personPath, true);
            service.AddFace(new Face.Face {FullPath = personFacePath, Encoding = result.Encoding, Name = dir });
        }
    }
}
