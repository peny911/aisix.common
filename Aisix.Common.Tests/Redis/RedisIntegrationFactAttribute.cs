using Xunit;

namespace Aisix.Common.Tests.Redis;

public sealed class RedisIntegrationFactAttribute : FactAttribute
{
    public RedisIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(RedisTestEnvironment.ConnectionStringVariableName)))
        {
            Skip = $"Set {RedisTestEnvironment.ConnectionStringVariableName} to run Redis integration tests.";
        }
    }
}

