using System;

namespace HikConsole.Events
{
    public class ExceptionEventArgs : EventArgs
    {
        public Exception Exception { get; set; }
    }
}
