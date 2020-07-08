using System;
using HikConsole.DTO.Contracts;

namespace HikConsole.Events
{
    public class VideoEventArgs : EventArgs
    {
        public VideoDTO Video { get; set; }
    }
}
