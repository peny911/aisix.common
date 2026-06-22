using System.Net;
using System.Text;

namespace Aisix.Common.Utils
{
    /// <summary>
    /// URL Query 参数处理工具。
    /// </summary>
    public static class UrlQueryHelper
    {
        /// <summary>
        /// 替换绝对 URL 中指定 query 参数的值，默认使用大小写敏感的参数名匹配。
        /// 仅替换目标参数值，不会重新编码整条 URL，可保留原始 percent-encoding 的大小写形态。
        /// </summary>
        /// <param name="url">待处理的绝对 URL。</param>
        /// <param name="key">需要替换的 query 参数名。</param>
        /// <param name="value">替换后的参数值。</param>
        /// <param name="result">替换成功后的 URL。</param>
        /// <returns>仅当 URL 合法且原始 query 中存在指定参数时返回 true。</returns>
        public static bool TryReplaceQueryValue(string url, string key, string value, out string? result)
        {
            return TryReplaceQueryValue(url, key, value, StringComparison.Ordinal, out result);
        }

        /// <summary>
        /// 替换绝对 URL 中指定 query 参数的值，并允许调用方指定参数名比较方式。
        /// 仅替换目标参数值，不会重新编码整条 URL，可保留原始 percent-encoding 的大小写形态。
        /// </summary>
        /// <param name="url">待处理的绝对 URL。</param>
        /// <param name="key">需要替换的 query 参数名。</param>
        /// <param name="value">替换后的参数值。</param>
        /// <param name="comparison">参数名匹配时使用的字符串比较方式。</param>
        /// <param name="result">替换成功后的 URL；失败时为 null。</param>
        /// <returns>仅当 URL 合法且原始 query 中存在指定参数时返回 true。</returns>
        public static bool TryReplaceQueryValue(string url, string key, string value, StringComparison comparison, out string? result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(url)
                || string.IsNullOrWhiteSpace(key)
                || value == null
                || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }

            var queryStart = url.IndexOf('?');
            if (queryStart < 0)
            {
                return false;
            }

            var fragmentStart = url.IndexOf('#', queryStart + 1);
            var queryEnd = fragmentStart >= 0 ? fragmentStart : url.Length;
            if (queryEnd <= queryStart + 1)
            {
                return false;
            }

            var query = url.Substring(queryStart + 1, queryEnd - queryStart - 1);
            var replacedQuery = ReplaceQueryValue(query, key, value, comparison, out var replaced);
            if (!replaced)
            {
                return false;
            }

            result = string.Concat(url.AsSpan(0, queryStart + 1), replacedQuery, url.AsSpan(queryEnd));
            return true;
        }

        /// <summary>
        /// 在原始 query 字符串中替换目标参数值，避免 parse 后重新编码其它参数。
        /// </summary>
        private static string ReplaceQueryValue(string query, string targetKey, string targetValue, StringComparison comparison, out bool replaced)
        {
            replaced = false;
            var builder = new StringBuilder(query.Length + targetValue.Length);
            var segmentStart = 0;
            var encodedValue = Uri.EscapeDataString(targetValue);

            while (segmentStart <= query.Length)
            {
                var separatorIndex = query.IndexOf('&', segmentStart);
                var segmentEnd = separatorIndex >= 0 ? separatorIndex : query.Length;
                var segment = query.AsSpan(segmentStart, segmentEnd - segmentStart);

                if (TryGetQueryKey(segment, out var rawKey) && IsSameQueryKey(rawKey, targetKey, comparison))
                {
                    builder.Append(rawKey);
                    builder.Append('=');
                    builder.Append(encodedValue);
                    replaced = true;
                }
                else
                {
                    builder.Append(segment);
                }

                if (separatorIndex < 0)
                {
                    break;
                }

                builder.Append('&');
                segmentStart = separatorIndex + 1;
            }

            return builder.ToString();
        }

        /// <summary>
        /// 从 query 片段中读取参数名，兼容无等号的参数片段。
        /// </summary>
        private static bool TryGetQueryKey(ReadOnlySpan<char> segment, out string rawKey)
        {
            var equalIndex = segment.IndexOf('=');
            var keySpan = equalIndex >= 0 ? segment[..equalIndex] : segment;
            rawKey = keySpan.ToString();
            return rawKey.Length > 0;
        }

        /// <summary>
        /// 判断原始参数名是否匹配目标参数名，优先按原文匹配，必要时再按 URL 解码后的参数名匹配。
        /// </summary>
        private static bool IsSameQueryKey(string rawKey, string targetKey, StringComparison comparison)
        {
            if (string.Equals(rawKey, targetKey, comparison))
            {
                return true;
            }

            var decodedKey = WebUtility.UrlDecode(rawKey);
            return string.Equals(decodedKey, targetKey, comparison);
        }
    }
}
