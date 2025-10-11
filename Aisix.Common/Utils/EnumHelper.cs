namespace Aisix.Common.Utils
{
    public static class EnumHelper
    {
        /// <summary>
        /// 根据传入的字符串解析为指定枚举类型的枚举值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">枚举名称字符串</param>
        /// <returns>枚举类型 T 的对应枚举值</returns>
        /// <exception cref="ArgumentException">当解析失败时抛出异常</exception>
        public static T ParseEnum<T>(string value) where T : struct, Enum
        {
            if (Enum.TryParse<T>(value, true, out T result))
            {
                return result;
            }
            else
            {
                throw new ArgumentException($"无效的枚举名称: {value}", nameof(value));
            }
        }

        /// <summary>
        /// 根据传入的字符串解析为指定枚举类型对应的整型值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">枚举名称字符串</param>
        /// <returns>对应的枚举值（整型）</returns>
        /// <exception cref="ArgumentException">当解析失败时抛出异常</exception>
        public static int ParseEnumToInt<T>(string value) where T : struct, Enum
        {
            T enumValue = ParseEnum<T>(value);
            return Convert.ToInt32(enumValue);
        }

        /// <summary>
        /// 根据传入的整数值获取指定枚举类型的枚举实例
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="value">枚举对应的整数值</param>
        /// <returns>枚举类型 T 的枚举值</returns>
        /// <exception cref="ArgumentException">当传入的值不在枚举定义范围内时抛出异常</exception>
        public static T FromValue<T>(int value) where T : struct, Enum
        {
            if (!Enum.IsDefined(typeof(T), value))
            {
                throw new ArgumentException($"无效的枚举值: {value}", nameof(value));
            }
            return (T)Enum.ToObject(typeof(T), value);
        }
    }
}
