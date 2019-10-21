using System;
using System.Runtime.Serialization;

namespace HikConsole.Data
{
    [Serializable]
    public class SdkException : Exception
    {
        public SdkException(string message)
            : base(message)
        {
        }

        protected SdkException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
