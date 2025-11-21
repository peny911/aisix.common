namespace Aisix.Common.QCloud
{
    /// <summary>
    /// 腾讯云临时凭证结果
    /// </summary>
    public class CredentialsResuslt
    {
        /// <summary>
        /// 临时凭证信息
        /// </summary>
        public CredentialsItem? Credentials { get; set; }

        /// <summary>
        /// 过期时间（时间戳）
        /// </summary>
        public int ExpiredTime { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime Expiration { get; set; }

        /// <summary>
        /// 请求 ID
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// 开始时间（时间戳）
        /// </summary>
        public int StartTime { get; set; }

        /// <summary>
        /// 临时凭证项
        /// </summary>
        public class CredentialsItem
        {
            /// <summary>
            /// 临时访问令牌
            /// </summary>
            public string? Token { get; set; }

            /// <summary>
            /// 临时 SecretId
            /// </summary>
            public string? TmpSecretId { get; set; }

            /// <summary>
            /// 临时 SecretKey
            /// </summary>
            public string? TmpSecretKey { get; set; }
        }
    }
}
