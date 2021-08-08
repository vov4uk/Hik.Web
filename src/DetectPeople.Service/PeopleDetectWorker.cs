using DetectPeople.Face;
using DetectPeople.Face.Helpers;
using DetectPeople.Face.Retina;
using DetectPeople.YOLOv4;
using DetectPeople.YOLOv4.DataStructures;
using FaceRecognitionDotNet;
using Hik.DTO.Config;
using Hik.DTO.Message;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using NLog;
using OpenCvSharp;
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
        private readonly FaceDetection retinaFaceDetection;
        private readonly Face.Retina.FaceRecognition retinaFaceRecognition;
        private readonly Stopwatch sw = new Stopwatch();

        public PeopleDetectWorker()
        {
            _scorer = new Scorer();
            service = new FacialRecognitionService();
            service.Initialize();


            this.retinaFaceDetection = new FaceDetection();
            this.retinaFaceRecognition = new Face.Retina.FaceRecognition();
            retinaFaceRecognition.Initialize();

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
            logger.Info("PeopleDetectWorker stopped");
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

            sw.Restart();
            IReadOnlyList<ObjectDetectResult> objects = await _scorer.DetectObjectsAsync(msg.OldFilePath);
            sw.Stop();
            logger.Info($"DetectObjects done in {sw.ElapsedMilliseconds}ms.");

            if (objects.Any(IsPerson))
            {
                logger.Info($"{objects.Count} objects detected");

                var peoples = objects.Where(IsPerson);
                logger.Info($"{peoples.Count()} peoples detected");
                foreach (var result in peoples)
                {
                    sw.Restart();
                    DetectFaces_Retina(msg.OldFilePath, msg.NewFileName, result.BBox.Select(x => (int)x).ToArray());
                    sw.Stop();
                    logger.Info($"DetectFaces done in {sw.ElapsedMilliseconds}ms.");
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

        private void DetectFaces_FaceRecognitionDotNet(string filePath, string fileName, int[] BBox)
        {
            try
            {
                var tempImg = Path.GetTempFileName() + ".jpeg";
                ImageProcessingHelper.CreateImage(filePath, tempImg, new Location(BBox[0], BBox[1], BBox[2], BBox[3]));
                File.Copy(tempImg, $@"c:\tmp\{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}.jpg", true);
                var res = service.Search(tempImg);

                logger.Info($"{res.Count} faces detected");
                foreach (SearchResult result in res)
                {
                    var match = result.Matches.First();
                    logger.Info($" {match.Name} : {match.Confidence} {match.ImagePath}");

                    if (match.Confidence > 0.65)
                    {
                        var personPath = Path.Combine(Path.GetDirectoryName(match.ImagePath), Path.GetFileName(fileName));
                        SavePerson(tempImg, personPath, result);
                    }
                    else // new person
                    {
                        logger.Warn("Add new person");
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

        private void DetectFaces_Retina(string filePath, string fileName, int[] BBox)
        {
            try
            {
                var tempImg = Path.GetTempFileName() + ".jpg";
                ImageProcessingHelper.CreateImage(filePath, tempImg, new Location(BBox[0], BBox[1], BBox[2], BBox[3]));

                File.Copy(tempImg, $@"c:\tmp\{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}.jpg", true);


                var faces = retinaFaceDetection.OpenFile(tempImg);
                Console.WriteLine($"{faces.Count} Faces Detected - {filePath}");

                foreach (var face in faces)
                {

                    Mat myNewMat = new Mat(face.Mat, new Rect((int)face.Rect.X, (int)face.Rect.Y, (int)face.Rect.Width, (int)face.Rect.Height));
                    (global::Retina.RecFaceInfo, global::Retina.RecFaceInfo, double?) res = retinaFaceRecognition.Search(myNewMat);
                    var searchResult = res.Item1;
                    var best = res.Item2;
                    var coeficient = res.Item3;

                    Console.WriteLine($"{best.Label} : {coeficient}");

                    if (coeficient > 0.65)
                    {
                        var personPath = Path.Combine(PathHelper.ImagesFolder(), best.Label, Path.GetFileName(filePath));
                        SavePerson(tempImg, myNewMat, personPath, searchResult);
                    }
                    else // new person
                    {
                        logger.Warn("Add new person");
                        var newPersonPath = Path.Combine(PathHelper.ImagesFolder(), Guid.NewGuid().ToString(), Path.GetFileName(fileName));
                        Directory.CreateDirectory(Path.GetDirectoryName(newPersonPath));

                        SavePerson(tempImg, myNewMat, newPersonPath, searchResult);
                    }
                }
                File.Delete(tempImg);
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

        private void SavePerson(string originalPath, Mat originalFile, string personPath, Retina.RecFaceInfo result)
        {
            string personFacePath = Path.ChangeExtension(personPath,".png");
            var parts = personPath.Split(new char[] { '\\', '/' });
            var dir = parts[parts.Length - 2];

            originalFile.SaveImage(personFacePath);
            result.FilePath = personFacePath;
            result.Label = dir;
            result.Face = originalFile;

            File.Move(originalPath, personPath, true);

            retinaFaceRecognition.AddFace(result);

        }
    }
}
