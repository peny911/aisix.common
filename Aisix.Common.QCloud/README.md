# Aisix.Common.QCloud

Aisix.Common.QCloud 是一个腾讯云服务集成库，提供了腾讯云临时凭证服务和验证码功能。

## 功能特性

- 腾讯云临时凭证 (STS) 服务
- 腾讯云验证码服务
- 支持 .NET 7.0 和 .NET 8.0

## 安装

```bash
dotnet add package Aisix.Common.QCloud
```

## 依赖项

- Tencent.QCloud.Cos.Sts.Sdk (>= 3.0.5)

## 使用方法

### 1. 配置服务

```csharp
// 在 appsettings.json 中配置
{
  "TencentCloud": {
    "SecretId": "your-secret-id",
    "SecretKey": "your-secret-key",
    "Region": "ap-beijing",
    "Bucket": "your-bucket-name",
    "CaptchaAppId": "your-captcha-app-id",
    "CaptchaAppSecretKey": "your-captcha-app-secret-key"
  }
}

// 在 Startup.cs 或 Program.cs 中配置
builder.Services.Configure<TencentCloudOptions>(builder.Configuration.GetSection("TencentCloud"));
builder.Services.AddSingleton<TencentCloudService>();
```

### 2. 使用临时凭证服务

```csharp
public class FileUploadService
{
    private readonly TencentCloudService _tencentCloudService;
    
    public FileUploadService(TencentCloudService tencentCloudService)
    {
        _tencentCloudService = tencentCloudService;
    }
    
    public async Task<CredentialsResult> GetUploadCredentials()
    {
        var result = await _tencentCloudService.GetCredentialsAsync();
        return result;
    }
}
```

### 3. 使用验证码服务

```csharp
public class AccountController : ControllerBase
{
    private readonly TencentCloudService _tencentCloudService;
    
    public AccountController(TencentCloudService tencentCloudService)
    {
        _tencentCloudService = tencentCloudService;
    }
    
    [HttpPost("verify-captcha")]
    public async Task<IActionResult> VerifyCaptcha([FromBody] CaptchaRequest request)
    {
        var result = await _tencentCloudService.VerifyCaptchaAsync(
            request.Ticket, 
            request.RandomStr, 
            request.UserIp
        );
        
        if (result.CaptchaCode == 1)
        {
            return Ok(new { success = true });
        }
        
        return BadRequest(new { success = false, message = result.ErrorMessage });
    }
}
```

## 配置选项

### 配置说明

#### 必需配置

以下配置项在使用腾讯云服务时**必须**提供：

| 配置项 | 类型 | 说明 |
|--------|------|------|
| `SecretId` | string | 腾讯云 API 密钥 ID，用于身份验证 |
| `SecretKey` | string | 腾讯云 API 密钥 Key，用于身份验证 |

#### 可选配置

以下配置项为可选，根据使用的功能决定是否需要配置：

| 配置项 | 类型 | 说明 | 使用场景 |
|--------|------|------|---------|
| `captcha` | object | 验证码服务配置 | 使用验证码功能时必需 |
| `captcha.CaptchaAppId` | ulong | 验证码应用 ID | 验证码服务 |
| `captcha.AppSecretKey` | string | 验证码应用密钥 | 验证码服务 |

### 配置示例

#### 基础配置（仅使用 STS 临时凭证）

```json
{
  "TencentCloud": {
    "SecretId": "your-secret-id",
    "SecretKey": "your-secret-key"
  }
}
```

#### 完整配置（包含验证码服务）

```json
{
  "TencentCloud": {
    "SecretId": "your-secret-id",
    "SecretKey": "your-secret-key",
    "captcha": {
      "CaptchaAppId": 123456789,
      "AppSecretKey": "your-captcha-secret-key"
    }
  }
}
```

### 配置验证

如果缺少必需的配置项，应用在启动或调用相关服务时会抛出异常并给出明确的错误提示：

- ❌ 缺少 `SecretId` 或 `SecretKey`：无法进行身份验证
- ❌ 使用验证码功能但未配置 `captcha`：抛出 `InvalidOperationException`

### 安全建议

⚠️ **重要**：不要将敏感配置（SecretId、SecretKey）直接写入源代码或提交到版本控制系统。

推荐做法：
1. 使用 **用户机密** (User Secrets) 存储开发环境配置
2. 使用 **环境变量** 存储生产环境配置
3. 使用 **Azure Key Vault** 或其他密钥管理服务

```bash
# 使用用户机密（开发环境）
dotnet user-secrets set "TencentCloud:SecretId" "your-secret-id"
dotnet user-secrets set "TencentCloud:SecretKey" "your-secret-key"
```

## 许可证

MIT License