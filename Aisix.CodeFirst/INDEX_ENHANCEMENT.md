# CodeFirst 工具索引功能增强

## 背景

在优化 `/api/v1/SysLog/search` 接口时，需要为 `sys_log` 表添加索引。原有的 CodeFirst 工具只能检测字段变化，无法检测和创建索引。

## 问题描述

1. **无法检测索引差异**：CodeFirst 工具只对比字段变化，不检测索引
2. **无法创建索引**：`CodeFirst.InitTables()` 默认不创建索引

## 修改内容

### 1. 添加索引差异检测 (`GetIndexDifferences` 方法)

**文件**: `Services/CodeFirstService.cs`

通过反射获取实体类上的 `SugarIndexAttribute` 特性，与数据库中的索引进行对比。

**核心逻辑**:
```csharp
// 从实体特性中获取 SugarIndexAttribute
var attrs = entityType.GetCustomAttributesData();
foreach (var attr in attrs)
{
    if (attr.AttributeType.Name == "SugarIndexAttribute")
    {
        // 构造函数参数：IndexName, 字段名1, 字段名2, ..., OrderByType, isUnique
        var indexName = attr.ConstructorArguments[0].Value?.ToString();
        // 与数据库索引对比...
    }
}
```

### 2. 添加索引创建功能 (`CreateIndexesFromEntity` 方法)

**文件**: `Services/CodeFirstService.cs`

从实体特性中解析索引定义，生成并执行 `ALTER TABLE ... ADD INDEX` SQL。

**关键点**:
- 构造函数参数结构：索引名, 字段1, 字段2, ..., 字段N, OrderByType, isUnique
- 字段名是参数 1 到倒数第 3 个（跳过 OrderByType 和 isUnique）
- 使用 `DbMaintenance.GetIndexList()` 获取数据库已有索引，避免重复创建

### 3. 在表更新时调用索引创建

**文件**: `Services/CodeFirstService.cs` - `ExecuteNormalTableUpdate` 方法

```csharp
// 执行表结构更新
_db!.CodeFirst.InitTables(entity);

// 创建索引
CreateIndexesFromEntity(tableName, entity);
```

## 调试过程

### 问题1: 特性名称不匹配

- 原因：特性名称是 `SugarIndexAttribute` 而不是 `SugarIndex`
- 解决：通过 `attr.AttributeType.Name` 判断

### 问题2: 构造函数参数获取方式

- 原因：特性参数通过构造函数传递，不是命名参数
- 解决：使用 `attr.ConstructorArguments` 获取

### 问题3: 参数解析错误

- 原因：OrderByType 和 isUnique 被当作字段名处理
- 错误：`Key column 'False' doesn't exist in table`
- 解决：跳过最后两个参数（OrderByType 和 isUnique）

## 使用方法

1. 在实体类上添加 `SugarIndex` 特性：
   ```csharp
   [SugarTable("sys_log")]
   [SugarIndex("idx_created", nameof(created), OrderByType.Asc)]
   [SugarIndex("idx_method", nameof(method), OrderByType.Asc)]
   [SugarIndex("idx_identity", nameof(identity), OrderByType.Asc)]
   public class sys_log
   ```

2. 运行 CodeFirst 工具：
   ```bash
   make codefirst
   ```

3. 选择要更新的表，系统会自动检测并创建索引

## 相关文件

- `Services/CodeFirstService.cs` - 核心逻辑
- `OpenRTBAdx.Interface.Entities/sys_log.cs` - 索引示例

## 修改日期

2026-02-22
