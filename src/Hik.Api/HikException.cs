using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Hik.Api
{
    [Serializable]
    [ExcludeFromCodeCoverage]
    public class HikException : Exception
    {
        public uint ErrorCode { get; }
        public string ErrorMessage { get { return GetEnumDescription((HikError)ErrorCode); } }

        public HikException(string method, uint errorCode)
            : base(method)
        {
            ErrorCode = errorCode;
        }

        protected HikException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        private static string GetEnumDescription(HikError value)
        {
            string val = value.ToString();
            FieldInfo fi = value.GetType().GetField(val);

            if (fi.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] attributes && attributes.Any())
            {
                return attributes.First().Description;
            }

            return val;
        }
    }
}
