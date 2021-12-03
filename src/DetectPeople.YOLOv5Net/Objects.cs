using System;
using System.Linq;
using Yolov5Net.Scorer.Models;

namespace DetectPeople.YOLOv5Net
{
    public static class Objects
    {
        public static int[] GetIds(string[] labels)
        {
            return new YoloCocoP5Model().Labels.Where(x => labels.Contains(x.Name)).Select(x => x.Id).Distinct().ToArray();
        }
    }
}
