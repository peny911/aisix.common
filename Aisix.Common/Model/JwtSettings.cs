namespace Aisix.Common.Settings
{
    public class JwtSettings
    {
        public const string Jwt = "Jwt";

        public required string SecretKey { get; set; }
        public required string EncryptKey { get; set; }
        public required string Issuer { get; set; }
        public required string Audience { get; set; }
        public int NotBeforeMinutes { get; set; }
        public int ExpirationMinutes { get; set; }
    }
}
