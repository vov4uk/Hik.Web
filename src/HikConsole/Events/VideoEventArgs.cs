using System;
using HikConsole.DTO.Contracts;

namespace HikConsole.Events
{
    public class VideoEventArgs : EventArgs
    {
        public VideoEventArgs(VideoDTO video, CameraDTO camera)
        {
            this.Video = video;
            this.Camera = camera;
        }

        public VideoDTO Video { get; }

        public CameraDTO Camera { get; }
    }
}
