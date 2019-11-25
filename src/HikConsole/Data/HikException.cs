using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace HikConsole.Data
{
    [Serializable]
    [ExcludeFromCodeCoverage]
    public class HikException : Exception
    {
        public HikException(string message)
            : base(message)
        {
        }

        protected HikException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
