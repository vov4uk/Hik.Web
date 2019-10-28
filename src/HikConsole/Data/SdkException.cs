using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace HikConsole.Data
{
    [Serializable]
    [ExcludeFromCodeCoverage]
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
