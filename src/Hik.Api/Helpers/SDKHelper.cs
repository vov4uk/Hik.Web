using System;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace Hik.Api.Helpers
{
    internal static class SdkHelper
    {
        internal static T InvokeSDK<T>(Expression<Func<T>> func)
        {
            T result = func.Compile().Invoke();

            switch (result)
            {
                case int val when val < 0:
                case bool def when !def:
                    {
                        throw CreateException(func.ToString());
                    }
                default: return result;
            }
        }

        private static HikException CreateException(string method)
        {
            uint lastErrorCode = NET_DVR_GetLastError();
            return new HikException(method, lastErrorCode);
        }

        [DllImport(HikApi.DllPath)]
        private static extern uint NET_DVR_GetLastError();
    }
}
