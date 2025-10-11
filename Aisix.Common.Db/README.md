# Aisix.Common.Db

Aisix.Common.Db 是一个自用项目！  
基于 SqlSugar 的数据库访问层库，提供了仓储模式、工作单元和缓存功能。

## 功能特性

- 基于 SqlSugar ORM 的数据库操作
- 仓储模式实现
- 工作单元模式
- 内存缓存支持
- 通用服务基类
- 支持 .NET 7.0 和 .NET 8.0

## 安装

```bash
dotnet add package Aisix.Common.Db
```

## 依赖项

- SqlSugarCore (>= 5.1.4.110)
- SqlSugar.IOC (>= 2.0.0)
- System.Runtime.Caching (>= 8.0.1)
- Aisix.Common

## 使用方法

### 1. 配置 appsettings.json

在 `appsettings.json` 文件中添加 `DBS` 配置节：

```json
{
  "DBS": {
    "MutiDBConns": [
      {
        "ConnId": "default",
        "DbType": 0,
        "Connection": "Server=your-server;Port=3306;Database=your-db;Uid=your-username;Pwd=your-password;",
        "Enabled": true
      }
    ],
    "ConsoleSql": true
  }
}
```

#### DBS 配置参数说明

- **MutiDBConns**: 数据库连接配置数组
  - **ConnId**: 连接标识符，默认为 "default"
  - **DbType**: 数据库类型（枚举值）
    - 0 = MySQL, 1 = SqlServer, 2 = Sqlite, 3 = Oracle, 4 = PostgreSQL
    - 更多类型请参考 SqlSugar 文档
  - **Connection**: 数据库连接字符串
  - **Enabled**: 是否启用此连接（true/false）

- **ConsoleSql**: 是否在控制台输出 SQL 语句（true/false）

### 2. 配置服务

在 `Program.cs` 中添加 SqlSugar 服务：

```csharp
// 添加 SqlSugar 服务，会自动注册所有必要的服务
builder.Services.AddSqlSugarIOC(builder.Configuration);
```

> **注意**：`AddSqlSugarIOC` 方法会自动注册以下服务：
> - `ISqlSugarClient`
> - `IBaseRepository<>` 和 `BaseRepository<>`
> - `IBaseService<>` 和 `BaseService<>`
> - `IUnitOfWork` 和 `UnitOfWork`

### 3. 使用仓储

```csharp
// 定义仓储接口
public interface IUserRepository : IBaseRepository<User>
{
    Task<List<User>> GetActiveUsersAsync();
}

// 实现仓储
public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(ISqlSugarClient db) : base(db)
    {
    }

    public async Task<List<User>> GetActiveUsersAsync()
    {
        return await GetListAsync(u => u.IsActive);
    }
}

// 使用服务
public class UserService : BaseService<User>
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository) : base(userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<User>> GetActiveUsersAsync()
    {
        return await _userRepository.GetActiveUsersAsync();
    }
}
```

### 4. 使用工作单元

```csharp
public class OrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    public OrderService(IUnitOfWork unitOfWork, IOrderRepository orderRepository, IProductRepository productRepository)
    {
        _unitOfWork = unitOfWork;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
    }

    public async Task CreateOrderAsync(Order order)
    {
        await _unitOfWork.BeginTranAsync();
        try
        {
            await _orderRepository.AddAsync(order);
            await _productRepository.UpdateStockAsync(order.ProductId, -order.Quantity);
            await _unitOfWork.CommitTranAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTranAsync();
            throw;
        }
    }
}
```

## 高级用法

### DbContext 模式

除了仓储模式，您也可以使用传统的 DbContext 模式。详细示例请参考 `Aisix.Common.WebApi.Sample` 项目。

### 使用 InitializeDatabase

对于更复杂的初始化需求，可以使用 `Orm.InitializeDatabase` 方法。请参考示例项目了解具体用法。

## 许可证

MIT License