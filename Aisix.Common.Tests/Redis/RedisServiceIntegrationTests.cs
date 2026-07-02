using Aisix.Common.Redis;
using StackExchange.Redis;
using Xunit;

namespace Aisix.Common.Tests.Redis;

public sealed class RedisServiceIntegrationTests : IAsyncLifetime
{
    private readonly string _prefix = $"aisix-common-tests:{Guid.NewGuid():N}";
    private readonly RedisService _redisService;
    private readonly List<string> _keys = new();

    public RedisServiceIntegrationTests()
    {
        _redisService = RedisTestEnvironment.CreateService(_prefix);
        Log($"Create RedisService with test prefix '{_prefix}'.");
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        foreach (var key in _keys)
        {
            Log($"Remove test key '{key}'.");
            await _redisService.RemoveAsync(key);
        }

        _redisService.Dispose();
        Log("Dispose RedisService.");
    }

    [RedisIntegrationFact]
    public async Task StringMethods_WithPrefix_CanSetGetExpireAndRemove()
    {
        const string key = "string:basic";
        _keys.Add(key);
        Log($"Start string test with key '{key}'.");

        var setResult = await _redisService.SetAsync(key, "value", expireMinutes: 1);
        Log($"Set key '{key}', result={setResult}.");
        var value = await _redisService.GetAsync(key);
        Log($"Get key '{key}', value='{value}'.");
        var exists = await _redisService.KeyExistsAsync(key);
        var ttl = await _redisService.GetKeyExpireAsync(key);
        Log($"Check key '{key}', exists={exists}, ttl={ttl}.");
        var removed = await _redisService.RemoveAsync(key);
        var existsAfterRemove = await _redisService.KeyExistsAsync(key);
        Log($"Remove key '{key}', removed={removed}, existsAfterRemove={existsAfterRemove}.");

        Assert.True(setResult);
        Assert.Equal("value", value);
        Assert.True(exists);
        Assert.NotNull(ttl);
        Assert.True(ttl > TimeSpan.Zero);
        Assert.True(removed);
        Assert.False(existsAfterRemove);
    }

    [RedisIntegrationFact]
    public async Task HashMethods_CanWriteReadAndDeleteFields()
    {
        const string key = "hash:basic";
        _keys.Add(key);
        Log($"Start hash test with key '{key}'.");

        var fieldCreated = await _redisService.HashSetAsync(key, "field", "value", expireMinutes: 1);
        Log($"HashSet key '{key}' field 'field', created={fieldCreated}.");
        var value = await _redisService.HashGetAsync(key, "field");
        Log($"HashGet key '{key}' field 'field', value='{value}'.");
        var incremented = await _redisService.HashIncrementAsync(key, "count", 2);
        Log($"HashIncrement key '{key}' field 'count', value={incremented}.");
        var deleted = await _redisService.HashRemoveAsync(key, "field");
        var valueAfterDelete = await _redisService.HashGetAsync(key, "field");
        Log($"HashRemove key '{key}' field 'field', deleted={deleted}, valueAfterDelete='{valueAfterDelete}'.");

        Assert.True(fieldCreated);
        Assert.Equal("value", value);
        Assert.Equal(2, incremented);
        Assert.True(deleted);
        Assert.Equal(string.Empty, valueAfterDelete);
    }

    [RedisIntegrationFact]
    public async Task StreamMethods_CanAddReadAckAndDeleteMessages()
    {
        const string key = "stream:basic";
        const string groupName = "group";
        const string consumerName = "consumer";
        _keys.Add(key);
        Log($"Start stream test with key '{key}', group '{groupName}', consumer '{consumerName}'.");

        var groupCreated = await _redisService.StreamCreateConsumerGroupAsync(
            key,
            groupName,
            position: "0-0",
            createStream: true);
        Log($"Create stream consumer group, created={groupCreated}.");

        var messageId = await _redisService.StreamAddAsync(
            key,
            new[]
            {
                new NameValueEntry("request_id", "req_1"),
                new NameValueEntry("dsp_code", "yyb"),
                new NameValueEntry("origin_price_cpm", "120")
            });
        Log($"XADD stream message, messageId={messageId}.");

        var entries = await _redisService.StreamReadGroupAsync(
            key,
            groupName,
            consumerName,
            position: ">",
            count: 10);
        Log($"XREADGROUP new messages, count={entries.Length}.");

        var pendingEntries = await _redisService.StreamReadGroupAsync(
            key,
            groupName,
            consumerName,
            position: "0-0",
            count: 10);
        Log($"XREADGROUP pending messages, count={pendingEntries.Length}.");

        var acknowledged = await _redisService.StreamAcknowledgeAsync(key, groupName, messageId);
        var deleted = await _redisService.StreamDeleteAsync(key, new[] { messageId });
        Log($"XACK message, acknowledged={acknowledged}. XDEL message, deleted={deleted}.");

        Assert.True(groupCreated);
        Assert.False(messageId.IsNullOrEmpty);
        Assert.Single(entries);
        Assert.Equal(messageId, entries[0].Id);
        Assert.Contains(entries[0].Values, item => item.Name == "request_id" && item.Value == "req_1");
        Assert.Single(pendingEntries);
        Assert.Equal(messageId, pendingEntries[0].Id);
        Assert.Equal(1, acknowledged);
        Assert.Equal(1, deleted);
    }

    [RedisIntegrationFact]
    public async Task StreamCreateConsumerGroup_WhenGroupAlreadyExists_ThrowsRedisServerException()
    {
        const string key = "stream:duplicate-group";
        const string groupName = "group";
        _keys.Add(key);
        Log($"Start duplicate consumer group test with key '{key}', group '{groupName}'.");

        var created = await _redisService.StreamCreateConsumerGroupAsync(
            key,
            groupName,
            position: "0-0",
            createStream: true);
        Log($"Create stream consumer group first time, created={created}.");

        var exception = await Assert.ThrowsAsync<RedisServerException>(() =>
            _redisService.StreamCreateConsumerGroupAsync(
                key,
                groupName,
                position: "0-0",
                createStream: true));
        Log($"Create stream consumer group second time, exception='{exception.Message}'.");

        Assert.True(created);
        Assert.Contains("BUSYGROUP", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static void Log(string message)
    {
        if (!RedisTestEnvironment.IsVerbose)
        {
            return;
        }

        Console.WriteLine($"[RedisIntegrationTest] {message}");
    }
}
