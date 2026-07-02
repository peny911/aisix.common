using Aisix.Common.Redis;

namespace Aisix.Common.Tests.Redis;

internal static class RedisTestEnvironment
{
    public const string ConnectionStringVariableName = "REDIS_TEST_CONNECTION_STRING";
    public const string VerboseVariableName = "REDIS_TEST_VERBOSE";

    public static bool IsVerbose
    {
        get
        {
            var value = Environment.GetEnvironmentVariable(VerboseVariableName);
            return value != null
                && (value.Equals("true", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("1", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("on", StringComparison.OrdinalIgnoreCase));
        }
    }

    public static RedisService CreateService(string prefix)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariableName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"{ConnectionStringVariableName} is not configured.");
        }

        return new RedisService(new RedisSettings
        {
            ConnectionString = connectionString,
            DefaultPrefix = prefix,
            DefaultDatabase = 0,
            SupportDatabaseSwitching = true
        });
    }
}
