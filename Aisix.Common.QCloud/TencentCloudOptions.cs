namespace Aisix.Common.QCloud
{
    public class TencentCloudOptions
    {
        public const string TencentCloud = "TencentCloud";

        public string SecretId { get; set; }
        public string SecretKey { get; set; }
        public Captcha captcha { get; set; }

        public class Captcha
        {
            public ulong CaptchaAppId { get; set; }
            public string AppSecretKey { get; set; }
        }
    }
}
