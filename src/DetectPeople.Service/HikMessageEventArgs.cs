using Hik.DTO.Message;
using System;

namespace DetectPeople.Service
{
    public class HikMessageEventArgs : EventArgs
    {
        public HikMessageEventArgs(DetectPeopleMessage msg)
        {
            this.Message = msg;
        }

        public DetectPeopleMessage Message { get; set; }
    }
}
