namespace Aisix.Common.QCloud
{
    /// <summary>
    /// 腾讯云配置选项
    /// </summary>
    public class TencentCloudOptions
    {
        public const string TencentCloud = "TencentCloud";

        /// <summary>
        /// 腾讯云 SecretId（必需）
        /// </summary>
        public string? SecretId { get; set; }

        /// <summary>
        /// 腾讯云 SecretKey（必需）
        /// </summary>
        public string? SecretKey { get; set; }

        /// <summary>
        /// 验证码配置（可选）
        /// </summary>
        public Captcha? captcha { get; set; }

        /// <summary>
        /// 验证码配置
        /// </summary>
        public class Captcha
        {
            /// <summary>
            /// 验证码应用 ID
            /// </summary>
            public ulong CaptchaAppId { get; set; }

            /// <summary>
            /// 验证码应用密钥（必需）
            /// </summary>
            public string? AppSecretKey { get; set; }
        }
    }
}
