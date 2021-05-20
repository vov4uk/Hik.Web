using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Hik.Api.Struct
{
    [ExcludeFromCodeCoverage]
    [StructLayout(LayoutKind.Sequential)]
    internal struct NET_DVR_TIME
    {
        public int dwYear;
        public int dwMonth;
        public int dwDay;
        public int dwHour;
        public int dwMinute;
        public int dwSecond;

        public NET_DVR_TIME(DateTime dateTime)
        {
            this.dwYear = dateTime.Year;
            this.dwMonth = dateTime.Month;
            this.dwDay = dateTime.Day;
            this.dwHour = dateTime.Hour;
            this.dwMinute = dateTime.Minute;
            this.dwSecond = dateTime.Second;
        }

        public override string ToString()
        {
            return $"{this.dwYear:0000}-{this.dwMonth:00}-{this.dwDay:00}_{this.dwHour:00}:{this.dwMinute:00}:{this.dwSecond:00}";
        }

        public DateTime ToDateTime()
        {
            return new DateTime(this.dwYear, this.dwMonth, this.dwDay, this.dwHour, this.dwMinute, this.dwSecond);
        }
    }
}
