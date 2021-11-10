using DetectPeople.YOLOv4.Assets;
using DetectPeople.YOLOv4.DataStructures;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Onnx;
using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;

namespace DetectPeople.YOLOv4
{
    // Author https://github.com/BobLd/YOLOv4MLNet
    public class ObjectsDetecor
    {
        protected readonly ILogger logger = LogManager.GetCurrentClassLogger();

        // model is available here:
        // https://github.com/onnx/models/tree/master/vision/object_detection_segmentation/yolov4
        private const string modelPath = @"Assets\yolov4.onnx";

        private readonly PredictionEngine<BitmapData, Prediction> predictionEngine;

        public ObjectsDetecor()
        {
            MLContext mlContext = new MLContext();

            // Define scoring pipeline
            EstimatorChain<OnnxTransformer> pipeline = mlContext.Transforms.ResizeImages(inputColumnName: "bitmap", outputColumnName: "input_1:0", imageWidth: 416, imageHeight: 416, resizing: ResizingKind.IsoPad)
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input_1:0", scaleImage: 1f / 255f, interleavePixelColors: true))
                .Append(mlContext.Transforms.ApplyOnnxModel(
                    shapeDictionary: new Dictionary<string, int[]>()
                    {
                        { "input_1:0",    new[] { 1, 416, 416, 3 } },
                        { "Identity:0",   new[] { 1, 52, 52, 3, 85 } },
                        { "Identity_1:0", new[] { 1, 26, 26, 3, 85 } },
                        { "Identity_2:0", new[] { 1, 13, 13, 3, 85 } },
                    },
                    inputColumnNames: new[]
                    {
                        "input_1:0"
                    },
                    outputColumnNames: new[]
                    {
                        "Identity:0",
                        "Identity_1:0",
                        "Identity_2:0"
                    },
                    modelFile: GetModelPath,
                    recursionLimit: 100));

            // model is available here:
            // https://github.com/onnx/models/tree/master/vision/object_detection_segmentation/yolov4

            // Fit on empty list to obtain input data schema
            TransformerChain<OnnxTransformer> model = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new List<BitmapData>()));

            // Create prediction engine
            predictionEngine = mlContext.Model.CreatePredictionEngine<BitmapData, Prediction>(model);
        }

        private string GetModelPath => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), modelPath);

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