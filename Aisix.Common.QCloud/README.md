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

### TencentCloudOptions

- `SecretId`: 腾讯云 SecretId
- `SecretKey`: 腾讯云 SecretKey
- `Region`: 地域信息
- `Bucket`: 存储桶名称
- `CaptchaAppId`: 验证码应用ID
- `CaptchaAppSecretKey`: 验证码应用密钥

## 许可证

MIT License