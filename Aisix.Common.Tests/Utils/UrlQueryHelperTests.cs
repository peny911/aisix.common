using Aisix.Common.Utils;
using Xunit;
using Assert = Xunit.Assert;

namespace Aisix.Common.Tests.Utils
{
    /// <summary>
    /// URL Query 参数处理工具测试。
    /// </summary>
    public class UrlQueryHelperTests
    {
        /// <summary>
        /// 验证替换目标参数时，不会重新编码 URL 中其它参数。
        /// </summary>
        [Fact]
        public void TryReplaceQueryValue_ReplaceTargetValue_PreservesOriginalEncodingCase()
        {
            // Arrange
            var url = "https://example.com/cb?path=a%2fb%2bc&event_type=2&token=x%3d#frag";

            // Act
            var success = UrlQueryHelper.TryReplaceQueryValue(url, "event_type", "-110", out var result);

            // Assert
            Assert.True(success);
            Assert.Equal("https://example.com/cb?path=a%2fb%2bc&event_type=-110&token=x%3d#frag", result);
        }

        /// <summary>
        /// 验证重复参数会被全部替换，保持现有调用方对多值参数的处理语义。
        /// </summary>
        [Fact]
        public void TryReplaceQueryValue_DuplicateTargetKey_ReplacesAllValues()
        {
            // Arrange
            var url = "https://example.com/cb?event_type=1&x=2&event_type=3";

            // Act
            var success = UrlQueryHelper.TryReplaceQueryValue(url, "event_type", "-110", out var result);

            // Assert
            Assert.True(success);
            Assert.Equal("https://example.com/cb?event_type=-110&x=2&event_type=-110", result);
        }

        /// <summary>
        /// 验证默认参数名匹配区分大小写，避免误替换大小写不同的业务参数。
        /// </summary>
        [Fact]
        public void TryReplaceQueryValue_DefaultComparison_IsCaseSensitive()
        {
            // Arrange
            var url = "https://example.com/cb?Event_Type=2";

            // Act
            var success = UrlQueryHelper.TryReplaceQueryValue(url, "event_type", "-110", out var result);

            // Assert
            Assert.False(success);
            Assert.Null(result);
        }

        /// <summary>
        /// 验证调用方可以显式指定忽略大小写的参数名匹配方式。
        /// </summary>
        [Fact]
        public void TryReplaceQueryValue_IgnoreCaseComparison_ReplacesMatchedValue()
        {
            // Arrange
            var url = "https://example.com/cb?Event_Type=2";

            // Act
            var success = UrlQueryHelper.TryReplaceQueryValue(url, "event_type", "-110", StringComparison.OrdinalIgnoreCase, out var result);

            // Assert
            Assert.True(success);
            Assert.Equal("https://example.com/cb?Event_Type=-110", result);
        }

        /// <summary>
        /// 验证无等号的目标参数也会被替换为标准 key=value 形式。
        /// </summary>
        [Fact]
        public void TryReplaceQueryValue_TargetWithoutEqual_ReplacesWithValue()
        {
            // Arrange
            var url = "https://example.com/cb?a&event_type&b=1";

            // Act
            var success = UrlQueryHelper.TryReplaceQueryValue(url, "event_type", "-110", out var result);

            // Assert
            Assert.True(success);
            Assert.Equal("https://example.com/cb?a&event_type=-110&b=1", result);
        }

        /// <summary>
        /// 验证只出现在 fragment 中的参数不会被误认为 query 参数。
        /// </summary>
        [Fact]
        public void TryReplaceQueryValue_KeyOnlyInFragment_ReturnsFalse()
        {
            // Arrange
            var url = "https://example.com/cb?a=1#event_type=2";

            // Act
            var success = UrlQueryHelper.TryReplaceQueryValue(url, "event_type", "-110", out var result);

            // Assert
            Assert.False(success);
            Assert.Null(result);
        }

        /// <summary>
        /// 验证目标参数名被 URL 编码时，仍能按解码后的参数名匹配并替换。
        /// </summary>
        [Fact]
        public void TryReplaceQueryValue_EncodedTargetKey_ReplacesMatchedValue()
        {
            // Arrange
            var url = "https://example.com/cb?%65vent_type=2";

            // Act
            var success = UrlQueryHelper.TryReplaceQueryValue(url, "event_type", "-110", out var result);

            // Assert
            Assert.True(success);
            Assert.Equal("https://example.com/cb?%65vent_type=-110", result);
        }

        /// <summary>
        /// 验证替换值包含中文和特殊字符时，会按标准 URL 编码写入目标参数值。
        /// </summary>
        [Fact]
        public void TryReplaceQueryValue_ValueContainsChineseAndSpecialChars_EncodesTargetValue()
        {
            // Arrange
            var url = "https://example.com/cb?event_type=2&path=a%2fb";

            // Act
            var success = UrlQueryHelper.TryReplaceQueryValue(url, "event_type", "点击+确认", out var result);

            // Assert
            Assert.True(success);
            Assert.Equal("https://example.com/cb?event_type=%E7%82%B9%E5%87%BB%2B%E7%A1%AE%E8%AE%A4&path=a%2fb", result);
        }

        /// <summary>
        /// 验证目标参数原本为空值时，也会被正常替换。
        /// </summary>
        [Fact]
        public void TryReplaceQueryValue_TargetEmptyValue_ReplacesMatchedValue()
        {
            // Arrange
            var url = "https://example.com/cb?event_type=";

            // Act
            var success = UrlQueryHelper.TryReplaceQueryValue(url, "event_type", "-110", out var result);

            // Assert
            Assert.True(success);
            Assert.Equal("https://example.com/cb?event_type=-110", result);
        }

        /// <summary>
        /// 验证仅包含问号但没有 query 内容时返回 false。
        /// </summary>
        [Fact]
        public void TryReplaceQueryValue_QueryOnlyQuestionMark_ReturnsFalse()
        {
            // Arrange
            var url = "https://example.com/cb?";

            // Act
            var success = UrlQueryHelper.TryReplaceQueryValue(url, "event_type", "-110", out var result);

            // Assert
            Assert.False(success);
            Assert.Null(result);
        }

        /// <summary>
        /// 验证替换值为 null 时返回 false，避免公共 API 在异常输入下抛出异常。
        /// </summary>
        [Fact]
        public void TryReplaceQueryValue_NullValue_ReturnsFalse()
        {
            // Arrange
            var url = "https://example.com/cb?event_type=2";

            // Act
            var success = UrlQueryHelper.TryReplaceQueryValue(url, "event_type", null!, out var result);

            // Assert
            Assert.False(success);
            Assert.Null(result);
        }
    }
}
