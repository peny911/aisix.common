namespace Aisix.Common.Redis
{
    public class RedisSettings
    {
        public required string ConnectionString { get; set; }
        public string DefaultPrefix { get; set; } = string.Empty;
        public int DefaultDatabase { get; set; }
        public int DefaultExpirySeconds { get; set; }
        public bool SupportDatabaseSwitching { get; set; } = true;
    }
}
