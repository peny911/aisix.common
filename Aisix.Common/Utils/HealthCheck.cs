using Aisix.Common.Redis;

namespace Aisix.Common.Utils
{
    public interface IHealthCheck
    {
        Task<HealthCheckResult> CheckHealthAsync();
    }

    public class HealthCheckResult
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = "Healthy";
        public string? Description { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }

    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IRedisService _redisService;

        public RedisHealthCheck(IRedisService redisService)
        {
            _redisService = redisService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync()
        {
            try
            {
                var pingTime = await _redisService.PingAsync();
                
                return new HealthCheckResult
                {
                    IsHealthy = true,
                    Status = "Healthy",
                    Description = "Redis connection is healthy",
                    Data = new Dictionary<string, object>
                    {
                        { "PingTimeMs", pingTime.TotalMilliseconds },
                        { "Timestamp", DateTime.UtcNow }
                    }
                };
            }
            catch (Exception ex)
            {
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Status = "Unhealthy",
                    Description = $"Redis health check failed: {ex.Message}",
                    Data = new Dictionary<string, object>
                    {
                        { "Error", ex.Message },
                        { "ExceptionType", ex.GetType().Name },
                        { "Timestamp", DateTime.UtcNow }
                    }
                };
            }
        }
    }

    public static class ConfigurationValidator
    {
        public static void ValidateRedisSettings(object settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var type = settings.GetType();
            
            var connectionStringProperty = type.GetProperty("ConnectionString");
            if (connectionStringProperty != null)
            {
                var connectionString = connectionStringProperty.GetValue(settings) as string;
                if (string.IsNullOrEmpty(connectionString))
                    throw new ArgumentException("Redis connection string cannot be null or empty.");
            }

            var defaultDatabaseProperty = type.GetProperty("DefaultDatabase");
            if (defaultDatabaseProperty != null)
            {
                var defaultDatabase = defaultDatabaseProperty.GetValue(settings);
                if (defaultDatabase is int db && db < 0)
                    throw new ArgumentException("Default database must be >= 0.");
            }
        }

        public static void ValidateJwtSettings(object settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            var type = settings.GetType();
            
            var secretKeyProperty = type.GetProperty("SecretKey");
            if (secretKeyProperty != null)
            {
                var secretKey = secretKeyProperty.GetValue(settings) as string;
                if (string.IsNullOrEmpty(secretKey))
                    throw new ArgumentException("JWT secret key cannot be null or empty.");
                if (secretKey.Length < 16)
                    throw new ArgumentException("JWT secret key must be at least 16 characters long.");
            }

            var issuerProperty = type.GetProperty("Issuer");
            if (issuerProperty != null)
            {
                var issuer = issuerProperty.GetValue(settings) as string;
                if (string.IsNullOrEmpty(issuer))
                    throw new ArgumentException("JWT issuer cannot be null or empty.");
            }

            var audienceProperty = type.GetProperty("Audience");
            if (audienceProperty != null)
            {
                var audience = audienceProperty.GetValue(settings) as string;
                if (string.IsNullOrEmpty(audience))
                    throw new ArgumentException("JWT audience cannot be null or empty.");
            }
        }
    }
}