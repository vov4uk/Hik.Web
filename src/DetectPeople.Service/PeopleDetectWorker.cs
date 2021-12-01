using DetectPeople.Face.Helpers;
using DetectPeople.Face.Retina;
using DetectPeople.YOLOv4;
using DetectPeople.YOLOv4.Assets;
using DetectPeople.YOLOv4.DataStructures;
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
using System.Drawing;
using System.Drawing.Imaging;
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
        private readonly ObjectsDetecor objectsDetector;
        private readonly FaceDetectionAdv faceDetect;
        private readonly FaceRecognition faceRec;
        private readonly Stopwatch timer = new();
        private PeopleDetectConfig config;
        private HashSet<int> allowedObjects;

        public PeopleDetectWorker()
        {
            try
            {
                this.objectsDetector = new ObjectsDetecor();
                this.faceDetect = new FaceDetectionAdv();
                this.faceRec = new FaceRecognition();
                faceRec.Initialize();
            }
            catch (Exception e)
            {
                logger.Error(e);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.Info("PeopleDetectWorker started");
            config = GetConfig();

            var objectIds = config.AllowedObjects.Select(x => Array.IndexOf(Objects.ClassesNames, x)).Distinct();
            allowedObjects = new HashSet<int>(objectIds);

            RabbitMQHelper hikReceiver = new(config.RabbitMQ.HostName, config.RabbitMQ.QueueName, config.RabbitMQ.RoutingKey);
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

        private PeopleDetectConfig GetConfig()
        {
            var rootDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var configPath = Path.Combine(rootDir, "config.json");
            if (!File.Exists(configPath))
            {
                logger.Error($"\"{configPath}\" does not exist.");
                logger.Info("Use default config");
                return new PeopleDetectConfig { RabbitMQ = new RabbitMQConfig { HostName = "localhost", QueueName = "hik", RoutingKey = "hik" } };
            }
            else
            {
                return JsonConvert.DeserializeObject<PeopleDetectConfig>(File.ReadAllText(configPath));
            }
        }

        private void DetectFaces(string filePath, string fileName)
        {
            var detectedObjectFolder = $@"c:\tmp\{ DateTime.Today.ToFileTime()}\";
            if (!Directory.Exists(detectedObjectFolder))
            {
                Directory.CreateDirectory(detectedObjectFolder);
            }

            var detectedJunkFolder = $@"c:\tmp\{ DateTime.Today.ToFileTime()}\junk";
            if (!Directory.Exists(detectedJunkFolder))
            {
                Directory.CreateDirectory(detectedJunkFolder);
            }

            var faces = faceDetect.DetectFaces(filePath);
            logger.Info($"{faces.Count} Faces Detected - {filePath}");

            foreach (var face in faces)
            {
                if (face.EyePointsScore < 0.75f)
                {
                    logger.Warn($"Eye score : {face.EyePointsScore}");
                    FaceDetectionAdv.SaveFaceImg(face, Cv2.ImRead(filePath), Path.Combine(detectedJunkFolder, $"eyescore_{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}.jpg"));
                    continue;
                }


                if (!(IsNormal(face.Head.Yaw) && IsNormal(face.Head.Pitch) && IsNormal(face.Head.Roll)))
                {
                    logger.Warn($"Yaw : {Math.Round(face.Head.Yaw, 2)}, Pitch : {Math.Round(face.Head.Pitch, 2)}, Roll : {Math.Round(face.Head.Roll, 2)}");
                    FaceDetectionAdv.SaveFaceImg(face, Cv2.ImRead(filePath), Path.Combine(detectedJunkFolder, $"ypr_{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}.jpg"));
                    continue;
                }

                if (!IsNormal(FaceDetectionAdv.Angle(face.Head.Axis[0])))
                {
                    logger.Warn("RED Head Axis > 20");
                    FaceDetectionAdv.SaveFaceImg(face, Cv2.ImRead(filePath), Path.Combine(detectedJunkFolder, $"red_{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}.jpg"));
                    continue;
                }

                FaceDetectionAdv.SaveFaceImg(face, Cv2.ImRead(filePath), Path.Combine(detectedObjectFolder, $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}.jpg"));

                var recognizedFaces = faceRec.Recognize(face.Mat.Clone());
                var searchResult = recognizedFaces.Found;
                var best = recognizedFaces.Best;
                var coeficient = recognizedFaces.MaxDistance;
                var faceRectImg = new Mat(face.Mat, face.FaceRectangle);

                if (coeficient > config.FaceCoeficient)
                {
                    logger.Warn($"{best.Label} : {coeficient}");
                    var personPath = Path.Combine(PathHelper.ImagesFolder(), best.Label, fileName);
                    SavePerson(filePath, faceRectImg, personPath, searchResult);
                }
                else // new person
                {
                    logger.Warn($"New person : {coeficient}");
                    var newPersonPath = Path.Combine(PathHelper.ImagesFolder(), "unknown_" + Guid.NewGuid().ToString(), fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(newPersonPath));

                    SavePerson(filePath, faceRectImg, newPersonPath, searchResult);
                }
            }
        }

        private bool IsPerson(ObjectDetectResult res, int minHeight, int minWidht)
        {
            if (allowedObjects.Contains(res.Id))
            {
                var rect = res.GetRectangle();
                return rect.Height >= minHeight && rect.Width >= minWidht;
            }
            return false;
        }

        private async void Rabbit_Received(object sender, BasicDeliverEventArgs ea)
        {
            try
            {

                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                DetectPeopleMessage msg = JsonConvert.DeserializeObject<DetectPeopleMessage>(body);
                logger.Debug("[x] Received {0}", body);

                if (!File.Exists(msg.OldFilePath))
                {
                    logger.Warn($"{msg.OldFilePath} not exist");
                    return;
                }

                timer.Restart();
                IReadOnlyList<ObjectDetectResult> objects = await objectsDetector.DetectObjectsAsync(msg.OldFilePath);
                timer.Stop();
                logger.Info($"DetectObjects done in {timer.ElapsedMilliseconds}ms. {objects.Count} objects detected. {string.Join(", ", objects.Select(x => x.Label))}");
                
                if (objects.Any())
                {
                    int minHeight = config.MinPersonHeightPixel;
                    int minWidth = config.MinPersonWidthPixel;
                    using (Image img = Image.FromFile(msg.OldFilePath))
                    {
                        minHeight = Convert.ToInt32(img.Height * config.MinPersonHeightPersentage / 100.0);
                        minWidth = Convert.ToInt32(img.Width * config.MinPersonWidthPersentage / 100.0);
                    }

                    var peoples = objects.Where(x => IsPerson(x, minHeight, minWidth));
                    if (peoples.Any())
                    {
                        logger.Info($"{peoples.Count()} peoples detected");
                        if (config.DetectFaces)
                        {

                            timer.Restart();
                            DetectFaces(msg.OldFilePath, msg.NewFileName);
                            timer.Stop();
                            logger.Info($"DetectFaces done in {timer.ElapsedMilliseconds}ms.");
                        }

                        SaveJpg(msg.OldFilePath, msg.NewFilePath);
                    }
                    else
                    {
                        ProcessJunk(msg, objects);
                    }
                }                
                else
                {
                    ProcessJunk(msg, objects);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private void ProcessJunk(DetectPeopleMessage msg, IReadOnlyList<ObjectDetectResult> objects)
        {
            if (!msg.DeleteJunk)
            {
                if (config.DrawJunkObjects)
                {
                    DrawObjects(msg.OldFilePath, msg.JunkFilePath, objects);
                }
                else
                {
                    SaveJpg(msg.OldFilePath, msg.JunkFilePath);
                }
            }
            File.Delete(msg.OldFilePath);
        }

        private void SavePerson(string originalPath, Mat originalFile, string personPath, FaceRecognitionResult result)
        {
            string personFacePath = Path.ChangeExtension(personPath, ".png");
            var parts = personPath.Split(new char[] { '\\', '/' });
            var dir = parts[^2];

            originalFile.SaveImage(personFacePath);
            result.FilePath = personFacePath;
            result.Label = dir;
            result.Face = originalFile;

            File.Copy(originalPath, personPath, true);

            faceRec.AddFace(result);
        }

        private void DrawObjects(string originalPath, string destination, IReadOnlyList<ObjectDetectResult> results)
        {
            using (Bitmap bitmap = new Bitmap(originalPath))
            {
                if (results.Any())
                {
                    using (var g = Graphics.FromImage(bitmap))
                    {
                        foreach (var res in results)
                        {
                            // draw predictions
                            var x1 = res.BBox[0];
                            var y1 = res.BBox[1];
                            var x2 = res.BBox[2];
                            var y2 = res.BBox[3];
                            g.DrawRectangle(Pens.Red, x1, y1, x2 - x1, y2 - y1);
                            using (var brushes = new SolidBrush(Color.FromArgb(50, Color.Red)))
                            {
                                g.FillRectangle(brushes, x1, y1, x2 - x1, y2 - y1);
                            }

                            g.DrawString(res.Label + " " + res.Confidence.ToString("0.00"), new Font("Arial", 12), Brushes.Yellow, new PointF(x1, y1));
                        }
                    }
                }

                var parameters = GetCompressParameters();

                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }

                bitmap.Save(destination, parameters.jpgEncoder, parameters.myEncoderParameters);
            }
        }

        private (ImageCodecInfo jpgEncoder, EncoderParameters myEncoderParameters) GetCompressParameters()
        {
            var jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
            var myEncoderParameters = new EncoderParameters(1);
            myEncoderParameters.Param[0] = new EncoderParameter(myEncoder, 25L);

            return (jpgEncoder, myEncoderParameters);
        }

        private bool IsNormal(float f)
        {
            return Math.Abs(f) <= 25;
        }

        private void SaveJpg(string source, string destination)
        {
            try
            {
                CompressImage(source, destination);
                File.Delete(source);
            }
            catch (Exception ex)
            {
                logger.Error("Error saving file '" + destination + ex.ToString());
            }
        }

        private void CompressImage(string source, string destination)
        {
            using (Bitmap bitmap = new Bitmap(source))
            {
                var parameters = GetCompressParameters();

                if (File.Exists(destination))
                {
                    File.Delete(destination);
                }

                bitmap.Save(destination, parameters.jpgEncoder, parameters.myEncoderParameters);
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}