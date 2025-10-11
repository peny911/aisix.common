namespace Aisix.Common.Redis
{
    public class RedisSettings
    {
        public string ConnectionString { get; set; }
        public string DefaultPrefix { get; set; }
        public int DefaultDatabase { get; set; }
        public int DefaultExpirySeconds { get; set; }
    }
}
