using System;
using System.Text.RegularExpressions;

namespace Aisix.Common.Utils
{
    /// <summary>
    /// 常见字符串格式校验帮助类
    /// </summary>
    public static class StringValidator
    {
        // 中国大陆手机号：1 开头，第二位 3-9，后面 9 位数字
        private static readonly Regex PhoneRegex = new Regex(@"^1[3-9]\d{9}$", RegexOptions.Compiled);

        // 身份证号：15 位全数字，或 18 位（最后一位可为 X/x）
        private static readonly Regex IdCardRegex = new Regex(@"(^\d{15}$)|(^\d{17}[\dXx]$)", RegexOptions.Compiled);

        // 简单邮箱格式校验
        private static readonly Regex EmailRegex = new Regex(
            @"^[\w\.\-]+@([\w\-]+\.)+[A-Za-z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// 验证是否是有效的中国大陆手机号
        /// </summary>
        public static bool IsPhoneNumber(string input)
        {
            return !string.IsNullOrWhiteSpace(input) && PhoneRegex.IsMatch(input);
        }

        /// <summary>
        /// 验证是否是有效的中国身份证号（15 位或 18 位）
        /// </summary>
        public static bool IsIdCardNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (!IdCardRegex.IsMatch(input))
                return false;

            // 对于 18 位身份证，可进一步做校验位验证（可选）
            if (input.Length == 18)
            {
                // 加权因子
                int[] weight = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
                // 校验码
                char[] checkCode = { '1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };

                int sum = 0;
                for (int i = 0; i < 17; i++)
                {
                    if (!char.IsDigit(input[i])) return false;
                    sum += (input[i] - '0') * weight[i];
                }
                char code = checkCode[sum % 11];
                return char.ToUpperInvariant(input[17]) == code;
            }

            return true;
        }

        /// <summary>
        /// 验证是否是合法的 HTTP/HTTPS URL
        /// </summary>
        public static bool IsUrl(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (Uri.TryCreate(input, UriKind.Absolute, out Uri uriResult))
            {
                return uriResult.Scheme == Uri.UriSchemeHttp
                    || uriResult.Scheme == Uri.UriSchemeHttps;
            }
            return false;
        }

        /// <summary>
        /// 验证是否是常见格式的邮箱地址
        /// </summary>
        public static bool IsEmail(string input)
        {
            return !string.IsNullOrWhiteSpace(input) && EmailRegex.IsMatch(input);
        }
    }
}
