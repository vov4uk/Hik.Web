using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Yolov5Net.Scorer;
using Yolov5Net.Scorer.Models;

namespace DetectPeople.YOLOv5Net
{
    public class ObjectsDetecor
    {
        private const string modelPath = @"Assets\yolov5n.onnx";
        private readonly YoloScorer<YoloCocoP5Model> scorer;
        private string GetModelPath => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), modelPath);
        public ObjectsDetecor()
        {
            scorer = new YoloScorer<YoloCocoP5Model>(GetModelPath);
        }

        public async Task<IReadOnlyList<ObjectDetectResult>> DetectObjectsAsync(string imagePath)
        {
            IReadOnlyList<ObjectDetectResult> results = Array.Empty<ObjectDetectResult>();
            using (var stream = new FileStream(imagePath, FileMode.Open))
            {
                using var image = Image.FromStream(stream);
                // predict
                var predictions = scorer.Predict(image);

                results = predictions.Select(x =>
                new ObjectDetectResult(x.Label.Id, new float[] { x.Rectangle.X, x.Rectangle.Y, x.Rectangle.Right, x.Rectangle.Bottom },
                x.Label.Name, x.Score)).ToArray();

                await stream.FlushAsync();
                await stream.DisposeAsync();
            }
            return results;
        }
    }
}
