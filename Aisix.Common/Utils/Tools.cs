using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Aisix.Common.Utils
{
    public static class Tools
    {
        /// <summary>
        /// 根据给定的概率（0-100），决定是否允许操作。
        /// </summary>
        /// <param name="rate"></param>
        /// <returns></returns>
        public static bool AllowByRate(int rate)
        {
            if (rate <= 0) return false;
            if (rate >= 100) return true;
            return Random.Shared.Next(0, 100) < rate;
        }

        /// <summary>
        /// Performs a deep copy of the object, using JSON Serialization.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>The copied object.</returns>
        public static T DeepCopy<T>(T source)
        {
            if (ReferenceEquals(source, null))
            {
                throw new ArgumentNullException(nameof(source), "The source object cannot be null.");
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                TypeInfoResolver = null,
                IncludeFields = true
            };
            string json = JsonSerializer.Serialize(source, options);
            return JsonSerializer.Deserialize<T>(json, options)!;
        }

        private static readonly char[] _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

        public static string GenerateRandomString(int length)
        {
            byte[] data = new byte[length];
            // 使用静态线程安全的 Fill 方法，不需要每次实例化 RNG 对象
            RandomNumberGenerator.Fill(data);

            var result = new StringBuilder(length);
            foreach (var b in data)
            {
                result.Append(_chars[b % _chars.Length]);
            }

            return result.ToString();
        }

        /// <summary>
        /// 提取文本开头符合“YYYY-MM-DD HH:MM:SS.fff”格式的日期字符串
        /// </summary>
        /// <param name="text">输入的文本行</param>
        /// <returns>匹配到的日期字符串，若未匹配到则返回 null</returns>
        public static string? ExtractDate(string text)
        {
            // 定义正则表达式，匹配格式如 "2025-03-24 18:19:05.589"
            string pattern = @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d+)";
            Match match = Regex.Match(text, pattern);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }
    }
}
