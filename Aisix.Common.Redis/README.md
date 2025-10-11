# Aisix.Common.Redis

Aisix.Common.Redis 是一个基于 StackExchange.Redis 的 Redis 客户端封装库，提供了简化的 Redis 操作接口。

## 功能特性

- 基于 StackExchange.Redis 的高性能操作
- 简化的 Redis 操作接口
- 支持字符串、哈希、列表、集合、有序集合等数据结构
- 连接池管理
- 支持 .NET 7.0

## 安装

```bash
dotnet add package Aisix.Common.Redis
```

## 依赖项

- StackExchange.Redis (>= 2.6.122)

## 使用方法

### 1. 配置服务

```csharp
// 在 appsettings.json 中配置
{
  "RedisSettings": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "Aisix_"
  }
}

// 在 Startup.cs 或 Program.cs 中配置
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("RedisSettings"));
builder.Services.AddSingleton<IRedisHelper, RedisHelper>();
```

### 2. 基本操作

```csharp
public class CacheService
{
    private readonly IRedisHelper _redisHelper;
    
    public CacheService(IRedisHelper redisHelper)
    {
        _redisHelper = redisHelper;
    }
    
    // 字符串操作
    public async Task SetValue(string key, string value)
    {
        await _redisHelper.StringSetAsync(key, value);
    }
    
    public async Task<string> GetValue(string key)
    {
        return await _redisHelper.StringGetAsync(key);
    }
    
    // 带过期时间的操作
    public async Task SetValueWithExpiry(string key, string value, TimeSpan expiry)
    {
        await _redisHelper.StringSetAsync(key, value, expiry);
    }
    
    // 删除操作
    public async Task<bool> DeleteKey(string key)
    {
        return await _redisHelper.KeyDeleteAsync(key);
    }
    
    // 检查键是否存在
    public async Task<bool> KeyExists(string key)
    {
        return await _redisHelper.KeyExistsAsync(key);
    }
}
```

### 3. 哈希操作

```csharp
public class UserCacheService
{
    private readonly IRedisHelper _redisHelper;
    
    public UserCacheService(IRedisHelper redisHelper)
    {
        _redisHelper = redisHelper;
    }
    
    // 设置哈希字段
    public async Task SetUserField(int userId, string field, string value)
    {
        await _redisHelper.HashSetAsync($"user:{userId}", field, value);
    }
    
    // 获取哈希字段
    public async Task<string> GetUserField(int userId, string field)
    {
        return await _redisHelper.HashGetAsync($"user:{userId}", field);
    }
    
    // 获取整个哈希
    public async Task<Dictionary<string, string>> GetUserAllFields(int userId)
    {
        return await _redisHelper.HashGetAllAsync($"user:{userId}");
    }
}
```

### 4. 列表操作

```csharp
public class MessageQueueService
{
    private readonly IRedisHelper _redisHelper;
    
    public MessageQueueService(IRedisHelper redisHelper)
    {
        _redisHelper = redisHelper;
    }
    
    // 添加到列表左侧
    public async Task EnqueueMessage(string queueName, string message)
    {
        await _redisHelper.ListLeftPushAsync(queueName, message);
    }
    
    // 从列表右侧获取
    public async Task<string> DequeueMessage(string queueName)
    {
        return await _redisHelper.ListRightPopAsync(queueName);
    }
    
    // 获取列表长度
    public async Task<long> GetQueueLength(string queueName)
    {
        return await _redisHelper.ListLengthAsync(queueName);
    }
}
```

## 配置选项

### RedisSettings

- `ConnectionString`: Redis 连接字符串
- `InstanceName`: 实例名称前缀

## 许可证

MIT License