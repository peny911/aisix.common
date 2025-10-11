using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aisix.Common.Utils
{
    public class DateTimeHelper
    {
        public static DateTime EPOCH => new DateTime(1970, 1, 1, 0, 0, 0, 0);

        public static DateTime ConvertToDateTime(string number)
        {
            string str = number.ToString();

            if (str.Length != 10)
            {
                throw new ArgumentException("The number must have 10 digits.");
            }

            int year = int.Parse(str.Substring(0, 4));
            int month = int.Parse(str.Substring(4, 2));
            int day = int.Parse(str.Substring(6, 2));
            int hour = int.Parse(str.Substring(8, 2));

            return new DateTime(year, month, day, hour, 0, 0);
        }

        public static long GetTimeStamp(bool isMilliseconds = false)
        {
            var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            long timestamp = Convert.ToInt64(isMilliseconds ? ts.TotalMilliseconds : ts.TotalSeconds);
            return timestamp;
        }

        public static long GetTimeStamp(DateTime time, bool isMilliseconds = false)
        {
            var ts = time.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            long timestamp = Convert.ToInt64(isMilliseconds ? ts.TotalMilliseconds : ts.TotalSeconds);
            return timestamp;
        }


        /// <summary>
        /// 将 DateTime 转换为时间戳（毫秒）。
        /// </summary>
        /// <param name="dt">要转换的 DateTime 对象。</param>
        /// <param name="timeZone">时区偏移量，默认为+8。</param>
        /// <returns>转换后的时间戳（毫秒）。</returns>
        public static long ConvertToTimestamp(DateTime dt, int timeZone = 8)
        {
            DateTime dtStart = EPOCH;
            TimeSpan toNow = dt.AddHours(-timeZone).Subtract(dtStart);
            return Convert.ToInt64(toNow.TotalMilliseconds);
        }

        /// <summary>
        /// 将时间戳（毫秒）转换为 DateTime 对象。
        /// </summary>
        /// <param name="timestamp">要转换的时间戳（毫秒）。</param>
        /// <param name="timeZone">时区偏移量，默认为+8。</param>
        /// <returns>转换后的 DateTime 对象。</returns>
        public static DateTime ConvertToDateTime(long timestamp, int timeZone = 8)
        {
            DateTime dtStart = EPOCH;
            TimeSpan toNow = TimeSpan.FromMilliseconds(timestamp);
            return dtStart.Add(toNow).AddHours(timeZone);
        }
    }
}
