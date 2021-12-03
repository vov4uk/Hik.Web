using DetectPeople.YOLOv5.Assets;
using Microsoft.ML;
using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using DetectPeople.YOLOv5.DataStructures;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;

namespace DetectPeople.YOLOv5
{
    //https://towardsdatascience.com/yolo-v4-optimal-speed-accuracy-for-object-detection-79896ed47b50
    public class ObjectsDetecor
    {
        protected readonly ILogger logger = LogManager.GetCurrentClassLogger();

        // model is available here:
        // https://github.com/onnx/models/tree/master/vision/object_detection_segmentation/yolov4
        const string modelPath = @"Assets\yolov5s_full_layer.onnx";

        private readonly PredictionEngine<BitmapData, Prediction> predictionEngine;

        public ObjectsDetecor()
        {
            MLContext mlContext = new MLContext();

            // Define scoring pipeline
            var pipeline = mlContext.Transforms.ResizeImages(inputColumnName: "bitmap", outputColumnName: "images", imageWidth: 640, imageHeight: 640, resizing: ResizingKind.Fill)
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "images", scaleImage: 1f / 255f, interleavePixelColors: false))
                .Append(mlContext.Transforms.ApplyOnnxModel(
                    shapeDictionary: new Dictionary<string, int[]>()
                    {
                        { "images", new[] { 1, 3, 640, 640 } },
                        { "output", new[] { 1, 25200, 85 } },
                    },
                    inputColumnNames: new[]
                    {
                        "images"
                    },
                    outputColumnNames: new[]
                    {
                        "output"
                    },
                    modelFile: modelPath));

            // Fit on empty list to obtain input data schema
            var model = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new List<BitmapData>()));

            // Create prediction engine
            predictionEngine = mlContext.Model.CreatePredictionEngine<BitmapData, Prediction>(model);

            // save model
            //mlContext.Model.Save(model, predictionEngine.OutputSchema, Path.ChangeExtension(modelPath, "zip"));
        }

        public async Task<IReadOnlyList<ObjectDetectResult>> DetectObjectsAsync(string imagePath)
        {
            IReadOnlyList<ObjectDetectResult> results = Array.Empty<ObjectDetectResult>();
            using (var stream = new FileStream(imagePath, FileMode.Open))
            {
                using (var bitmap = new Bitmap(stream))
                {
                    // predict
                    var predict = predictionEngine.Predict(new BitmapData() { Image = bitmap });
                    results = predict.GetResults(Objects.ClassesNames, 0.3f, 0.7f);
                }
                await stream.FlushAsync();
                await stream.DisposeAsync();
            }
            return results;
        }
    }
}
