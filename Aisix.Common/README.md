# Aisix.Common

Aisix.Common 是一个自用项目！  
通用的 .NET 开发工具库，提供了丰富的功能模块，包括 API 响应处理、工具类扩展、Redis 操作、IP 地理位置查询、权限验证等。

## 功能特性

### 🚀 API 响应处理
- 统一的 API 响应格式 (`ApiResult<T>`)
- 标准化的成功/失败响应
- 支持泛型类型的数据返回

### 🔧 工具类扩展
- **字符串扩展**: 字符串验证、数字转换、波斯语字符处理
- **加密解密**: AES 加密（随机IV）、SHA256 哈希、MD5 哈希（已过时）
- **日期时间**: 日期时间处理工具
- **网络工具**: HTTP 客户端帮助类（增强异常处理）、网络工具
- **ID 生成**: CUID 生成器（优化）、雪花算法 ID
- **权重选择**: 基于权重的随机选择器
- **健康检查**: Redis 健康检查、配置验证
- **随机数生成**: 优化的随机数生成器，避免重复值

### 🗄️ Redis 操作
- 完整的 Redis 客户端封装（增强版）
- 支持字符串、哈希、列表、集合、有序集合
- 批量操作和事务支持
- 键过期时间管理
- SCAN 操作支持（默认步长 1000）
- **新增功能**：
  - 连接池配置和超时设置
  - 连接失败和恢复事件监听
  - 实现 IDisposable 接口，正确释放资源
  - 增强的错误处理和连接管理
  - 支持健康检查和连接状态监控

### 🌍 IP 地理位置查询
- 基于 IP2Region 的 IP 地址地理位置查询
- 高性能的离线 IP 查询
- 支持 IPv4 地址定位

### 🔐 权限验证
- 基于 Policy 的权限验证
- 自定义权限策略提供程序
- 权限授权特性

### 📊 其他功能
- **AutoMapper 集成**: 自动映射配置和自定义映射
- **Swagger 增强**: 操作过滤器、路径版本控制
- **异常处理**: WebAPI 异常处理和状态码管理（增强版）
- **微信扩展**: 微信消息处理工具（增强异常处理）
- **线程池配置**: 线程池设置管理
- **配置验证**: Redis 和 JWT 配置验证工具
- **健康检查**: 系统健康检查和监控功能

## 安装

```bash
dotnet add package Aisix.Common
```

## 依赖项

- AutoMapper (>= 13.0.1)
- IP2Region.Net (>= 2.0.2)
- Newtonsoft.Json (>= 13.0.3)
- NLog (>= 5.3.4)
- Pluralize.NET (>= 1.0.2)
- StackExchange.Redis (>= 2.8.16)
- Swashbuckle.AspNetCore (>= 6.7.3)

## 使用方法

### 1. API 响应处理

```csharp
// 返回成功响应
return ApiResult.Success(data);

// 返回失败响应
return ApiResult.Fail("错误信息", 400);

// 返回泛型响应
return ApiResult<User>.Success(user);
```

### 2. 字符串扩展

```csharp
// 字符串验证
string text = "hello";
bool hasValue = text.HasValue(); // true

// 数字转换
string numberStr = "123";
int number = numberStr.ToInt(); // 123

// 波斯语字符处理
string persianText = "123";
string converted = persianText.En2Fa(); // "۱۲۳"
```

### 3. Redis 操作

```csharp
// 配置 Redis 服务
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("RedisSettings"));
builder.Services.AddSingleton<IRedisService, RedisService>();

// 使用 Redis
public class CacheService
{
    private readonly IRedisService _redisService;
    
    public CacheService(IRedisService redisService)
    {
        _redisService = redisService;
    }
    
    public async Task SetValue(string key, string value)
    {
        await _redisService.SetAsync(key, value, expireMinutes: 30);
    }
    
    public async Task<string?> GetValue(string key)
    {
        return await _redisService.GetAsync(key);
    }
}
```

#### Redis 集群模式支持

现在支持 Redis 集群模式和单机模式的自动适配：

**单机模式配置（支持数据库切换）：**
```json
{
  "RedisSettings": {
    "ConnectionString": "localhost:6379,password=yourpassword",
    "DefaultPrefix": "",
    "DefaultDatabase": 1,
    "DefaultExpirySeconds": 5184000,
    "SupportDatabaseSwitching": true
  }
}
```

**集群模式配置（不支持数据库切换）：**
```json
{
  "RedisSettings": {
    "ConnectionString": "10.0.0.2:6379,password=yourpassword",
    "DefaultPrefix": "",
    "DefaultDatabase": 1,
    "DefaultExpirySeconds": 5184000,
    "SupportDatabaseSwitching": false
  }
}
```

**特性说明：**
- `SupportDatabaseSwitching`: 默认为 `true`，表示支持数据库切换（单机模式）
- 当设置为 `false` 时，自动将 `DefaultDatabase` 添加到连接字符串中，适配 Redis 集群模式
- 在集群模式下，如果尝试切换到非默认数据库，会抛出 `NotSupportedException` 异常

### 4. IP 地理位置查询

```csharp
// 配置 IP2Region 服务
builder.Services.AddIP2Region();

// 使用 IP2Region
public class LocationService
{
    private readonly IIP2RegionSearcher _ipSearcher;
    
    public LocationService(IIP2RegionSearcher ipSearcher)
    {
        _ipSearcher = ipSearcher;
    }
    
    public GeoLocationInfo GetLocation(string ip)
    {
        return _ipSearcher.Search(ip);
    }
}
```

### 5. 权限验证

```csharp
// 使用权限验证特性
[PermissionAuthorize("user.create")]
public class UserController : ControllerBase
{
    [HttpPost]
    public IActionResult CreateUser([FromBody] User user)
    {
        // 创建用户逻辑
        return Ok();
    }
}
```

### 6. AutoMapper 配置

```csharp
// 配置 AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// 自定义映射
public class UserProfile : IHaveCustomMapping
{
    public void CreateMappings(Profile profile)
    {
        profile.CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
    }
}
```

## 配置选项

### RedisSettings
```json
{
  "RedisSettings": {
    "ConnectionString": "localhost:6379,password=yourpassword",
    "DefaultPrefix": "",
    "DefaultDatabase": 0,
    "DefaultExpirySeconds": 5184000,
    "SupportDatabaseSwitching": true
  }
}
```

**配置参数说明：**
- `ConnectionString`: Redis 连接字符串，支持密码和多个节点
- `DefaultPrefix`: 键前缀，用于区分不同应用的数据
- `DefaultDatabase`: 默认数据库编号（0-15）
- `DefaultExpirySeconds`: 默认过期时间（秒）
- `SupportDatabaseSwitching`: 是否支持数据库切换，`true` 为单机模式，`false` 为集群模式

### JwtSettings
```json
{
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "Issuer": "your-issuer",
    "Audience": "your-audience",
    "ExpirationMinutes": 120
  }
}
```

## 版本信息

- 当前版本: 1.0.5-beta.2
- 支持 .NET 8.0
- 开箱即用的 NuGet 包

## 🆕 最新更新 (1.0.5-beta.2)

### 🔒 安全性增强
- **AES 加密优化**: 使用随机IV替代固定IV，大幅提升安全性
- **SHA256 加密**: 新增SHA256加密方法，替代不安全的MD5
- **MD5 标记过时**: MD5方法已标记为过时，建议使用SHA256

### 🚀 性能优化
- **随机数生成**: 使用Random.Shared替代new Random()，避免重复值
- **Redis 连接管理**: 添加连接池配置、超时设置、事件监听
- **异步错误处理**: 为HttpClientHelper添加详细的异常处理

### 🛠️ 稳定性提升
- **异常处理**: 改进WxeMessager和HttpClientHelper的异常处理
- **资源管理**: 为RedisService正确实现IDisposable接口
- **连接监控**: 添加Redis连接失败和恢复事件监听

### 📊 新增功能
- **健康检查**: 新增Redis健康检查功能
- **配置验证**: 添加Redis和JWT配置验证工具
- **连接池**: Redis连接池配置和优化

### 📦 依赖更新
- .NET 版本统一升级到 8.0
- NLog: 5.2.7 → 5.3.4
- StackExchange.Redis: 2.6.122 → 2.8.16
- Swashbuckle.AspNetCore: 6.5.0 → 6.7.3

## 许可证

MIT License