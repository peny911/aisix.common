using SqlSugar;
using System.Text;
using System.Text.RegularExpressions;
using Aisix.Common.Db;

namespace Aisix.DbFirst.Services
{
    /// <summary>
    /// DbFirst 服务实现
    /// </summary>
    public class DbFirstService : IDbFirstService
    {
        private readonly ISqlSugarClient _db;
        private readonly DbFirstConfiguration _config;

        public DbFirstService(ISqlSugarClient db, DbFirstConfiguration config)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 运行 DbFirst 代码生成
        /// </summary>
        public void Run()
        {
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine($"{_config.SolutionName} 代码生成器 - DbFirst 模式");
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();

            if (_config.InteractiveMode)
            {
                RunInteractive();
            }
            else
            {
                RunBatch();
            }
        }

        /// <summary>
        /// 交互模式运行
        /// </summary>
        private void RunInteractive()
        {
            while (true)
            {
                var allTables = GetAllTables();

                Console.WriteLine($"发现 {allTables.Count} 张数据表：");
                Console.WriteLine();

                for (int i = 0; i < allTables.Count; i++)
                {
                    Console.WriteLine($"  [{i + 1}] {allTables[i]}");
                }

                Console.WriteLine();
                Console.WriteLine("请选择要生成的表：");
                Console.WriteLine("  - 输入表编号（如：1）生成单个表");
                Console.WriteLine("  - 输入多个编号（如：1,3,5）生成多张表");
                Console.WriteLine("  - 输入 'all' 或 'a' 生成全部表");
                Console.WriteLine("  - 输入 'q' 或 'quit' 退出");
                Console.WriteLine();
                Console.Write("请输入选择: ");

                var input = Console.ReadLine()?.Trim().ToLower();

                if (string.IsNullOrEmpty(input) || input == "q" || input == "quit")
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"感谢使用 {_config.SolutionName} 代码生成器，再见！");
                    Console.ResetColor();
                    return;
                }

                List<string> tablesToGenerate;

                if (input == "all" || input == "a")
                {
                    tablesToGenerate = allTables;
                    Console.WriteLine($"\n将生成全部 {allTables.Count} 张表的代码...\n");
                }
                else
                {
                    var indices = input.Split(',', '，')
                        .Select(s => s.Trim())
                        .Where(s => int.TryParse(s, out _))
                        .Select(s => int.Parse(s))
                        .Where(i => i > 0 && i <= allTables.Count)
                        .Distinct()
                        .OrderBy(i => i)
                        .ToList();

                    if (indices.Count == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n错误：未输入有效的表编号，请重新选择\n");
                        Console.ResetColor();
                        Console.WriteLine("=".PadRight(60, '='));
                        Console.WriteLine();
                        continue;
                    }

                    tablesToGenerate = indices.Select(i => allTables[i - 1]).ToList();
                    Console.WriteLine($"\n将生成以下 {tablesToGenerate.Count} 张表的代码：");
                    tablesToGenerate.ForEach(t => Console.WriteLine($"  - {t}"));
                    Console.WriteLine();
                }

                var results = GenerateTables(tablesToGenerate);
                PrintResults(results);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("按任意键继续...");
                Console.ResetColor();
                Console.ReadKey(true);
                Console.Clear();

                Console.WriteLine("=".PadRight(60, '='));
                Console.WriteLine($"{_config.SolutionName} 代码生成器 - DbFirst 模式");
                Console.WriteLine("=".PadRight(60, '='));
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 批量模式运行
        /// </summary>
        private void RunBatch()
        {
            var tablesToGenerate = _config.Tables.Count > 0 ? _config.Tables : GetAllTables();
            Console.WriteLine($"将生成 {tablesToGenerate.Count} 张表的代码...\n");

            var results = GenerateTables(tablesToGenerate);
            PrintResults(results);
        }

        /// <summary>
        /// 打印生成结果
        /// </summary>
        private void PrintResults(List<GenerateResult> results)
        {
            var successCount = results.Count(r => r.Success);
            var failCount = results.Count - successCount;

            Console.WriteLine("=".PadRight(60, '='));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"生成完成！成功: {successCount}, 失败: {failCount}");
            Console.ResetColor();
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine();
        }

        /// <summary>
        /// 获取所有可用的表
        /// </summary>
        public List<string> GetAllTables()
        {
            return _db.DbMaintenance.GetTableInfoList()
                .Select(it => it.Name)
                .Where(name => !IsTableExcluded(name))
                .ToList();
        }

        /// <summary>
        /// 生成指定表的代码
        /// </summary>
        public GenerateResult GenerateTable(string tableName)
        {
            var result = new GenerateResult { TableName = tableName };

            try
            {
                Console.WriteLine($"正在生成 [{tableName}] ...");

                result.EntityGenerated = CreateModel(tableName);
                Console.WriteLine($"  {(result.EntityGenerated ? "✓" : "✗")} 实体类: {(result.EntityGenerated ? "成功" : "失败")}");

                result.ServiceGenerated = CreateService(tableName);
                Console.WriteLine($"  {(result.ServiceGenerated ? "✓" : "✗")} 服务实现: {(result.ServiceGenerated ? "成功" : "失败")}");

                result.IServiceGenerated = CreateIService(tableName);
                Console.WriteLine($"  {(result.IServiceGenerated ? "✓" : "✗")} 服务接口: {(result.IServiceGenerated ? "成功" : "失败")}");

                result.Success = result.EntityGenerated && result.ServiceGenerated && result.IServiceGenerated;

                if (result.Success)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"  ✓ [{tableName}] 生成成功");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✗ [{tableName}] 部分生成失败");
                }
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ [{tableName}] 生成异常: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine();
            return result;
        }

        /// <summary>
        /// 批量生成表的代码
        /// </summary>
        public List<GenerateResult> GenerateTables(List<string> tableNames)
        {
            return tableNames.Select(GenerateTable).ToList();
        }

        #region 私有方法

        /// <summary>
        /// 判断表是否在排除列表中
        /// </summary>
        private bool IsTableExcluded(string tableName)
        {
            foreach (var pattern in _config.ExcludedTables)
            {
                if (IsWildcardMatch(tableName, pattern))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 通配符匹配
        /// </summary>
        private bool IsWildcardMatch(string text, string pattern)
        {
            if (!pattern.Contains('*'))
                return text.Equals(pattern, StringComparison.OrdinalIgnoreCase);

            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 创建实体类
        /// </summary>
        private bool CreateModel(string tableName)
        {
            try
            {
                var columns = _db.DbMaintenance.GetColumnInfosByTableName(tableName);
                var tableInfo = _db.DbMaintenance.GetTableInfoList().FirstOrDefault(t => t.Name == tableName);
                var tableDescription = tableInfo?.Description ?? tableName;
                var escapedTableDescription = tableDescription.Replace("\"", "\\\"");

                var sb = new StringBuilder();

                // 文件头
                sb.AppendLine("//------------------------------------------------------------------------------");
                sb.AppendLine("// <auto-generated>");
                sb.AppendLine("//     此代码已从模板生成，手动更改此文件可能导致应用程序出现意外的行为。");
                sb.AppendLine("//     如果重新生成代码，将覆盖对此文件的手动更改。");
                sb.AppendLine($"//     author {_config.Author}");
                sb.AppendLine($"//     生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine("// </auto-generated>");
                sb.AppendLine("//------------------------------------------------------------------------------");
                sb.AppendLine();
                sb.AppendLine("#nullable enable");
                sb.AppendLine();
                sb.AppendLine("using SqlSugar;");
                sb.AppendLine("using System;");
                sb.AppendLine();
                sb.AppendLine($"namespace {_config.GetEntityNamespace()}");
                sb.AppendLine("{");

                // 类注释
                sb.AppendLine("    /// <summary>");
                sb.AppendLine($"    /// {tableDescription}");
                sb.AppendLine("    /// </summary>");
                sb.AppendLine($"    [SugarTable(\"{tableName}\", TableDescription = \"{escapedTableDescription}\")]");

                // 生成索引特性
                GenerateIndexAttributes(sb, tableName);

                sb.AppendLine($"    public class {tableName}");
                sb.AppendLine("    {");

                // 生成属性
                foreach (var column in columns)
                {
                    GenerateProperty(sb, column, tableName);
                }

                sb.AppendLine("    }");
                sb.AppendLine("}");

                // 写入文件
                var fileName = Path.Combine(_config.EntityOutputPath, $"{tableName}.cs");
                EnsureDirectoryExists(_config.EntityOutputPath);
                File.WriteAllText(fileName, sb.ToString());

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    生成实体类时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 生成索引特性
        /// </summary>
        private void GenerateIndexAttributes(StringBuilder sb, string tableName)
        {
            try
            {
                var indexes = _db.DbMaintenance.GetIndexList(tableName);
                var distinctIndexes = indexes.Distinct().ToList();

                foreach (var indexName in distinctIndexes)
                {
                    if (indexName.ToLower().Contains("primary")) continue;

                    var indexColumns = GetIndexColumns(tableName, indexName);
                    if (indexColumns.Count > 0)
                    {
                        var indexParts = new List<string> { $"\"{indexName}\"" };

                        foreach (var colName in indexColumns)
                        {
                            indexParts.Add($"nameof({tableName}.{colName})");
                            indexParts.Add("OrderByType.Asc");
                        }

                        if (IsUniqueIndex(tableName, indexName))
                        {
                            indexParts.Add("isUnique: true");
                        }

                        sb.AppendLine($"    [SugarIndex({string.Join(", ", indexParts)})]");
                    }
                }
            }
            catch
            {
                // 忽略索引生成错误
            }
        }

        /// <summary>
        /// 生成属性
        /// </summary>
        private void GenerateProperty(StringBuilder sb, DbColumnInfo column, string tableName)
        {
            var columnDescription = column.ColumnDescription ?? "";
            var normalizedDefaultValue = NormalizeDefaultValue(column);
            // PostgreSQL 的 identity 列在部分场景下不会被 SqlSugar 正确标记，这里补一层兜底识别。
            var isIdentity = IsIdentityColumn(tableName, column);

            sb.AppendLine("        /// <summary>");
            sb.AppendLine($"        /// {columnDescription}");
            sb.AppendLine("        /// </summary>");

            var attrs = new List<string>();

            if (column.IsPrimarykey) attrs.Add("IsPrimaryKey = true");
            if (isIdentity) attrs.Add("IsIdentity = true");
            if (column.Length > 0 && IsStringType(column.DataType)) attrs.Add($"Length = {column.Length}");
            attrs.Add($"IsNullable = {column.IsNullable.ToString().ToLower()}");

            if (!string.IsNullOrEmpty(normalizedDefaultValue))
            {
                attrs.Add($"DefaultValue = \"{normalizedDefaultValue}\"");
            }

            if (NeedsDataTypeAttribute(column.DataType))
                attrs.Add($"ColumnDataType = \"{column.DataType.ToLower()}\"");

            if (!string.IsNullOrEmpty(columnDescription))
                attrs.Add($"ColumnDescription = \"{columnDescription.Replace("\"", "\\\"")}\"");

            if (attrs.Count > 0)
                sb.AppendLine($"        [SugarColumn({string.Join(", ", attrs)})]");

            var propertyType = GetCSharpType(column.DataType, column.IsNullable);
            var initValue = GetPropertyInitializer(column, normalizedDefaultValue);

            if (!string.IsNullOrEmpty(initValue))
                sb.AppendLine($"        public {propertyType} {column.DbColumnName} {{ get; set; }} = {initValue};");
            else
                sb.AppendLine($"        public {propertyType} {column.DbColumnName} {{ get; set; }}");

            sb.AppendLine();
        }

        /// <summary>
        /// 创建服务实现
        /// </summary>
        private bool CreateService(string tableName)
        {
            try
            {
                var className = $"{ToCamelCase(tableName)}Service";
                var saveFileName = $"{className}.cs";
                var filePath = Path.Combine(_config.ServiceOutputPath, saveFileName);

                string customCode = "";
                string classSummary = "";

                if (File.Exists(filePath))
                {
                    var existingContent = File.ReadAllText(filePath);
                    customCode = GetCustomValue(existingContent, "#region CustomInterface \n", "        #endregion\n");
                    classSummary = GetClassSummary(existingContent, className);
                }

                var sb = new StringBuilder();
                sb.AppendLine("//------------------------------------------------------------------------------");
                sb.AppendLine("// <auto-generated>");
                sb.AppendLine("//     此代码已从模板生成，手动更改此文件可能导致应用程序出现意外的行为。");
                sb.AppendLine("//     如果重新生成代码，将覆盖对此文件的手动更改。");
                sb.AppendLine($"//     author {_config.Author}");
                sb.AppendLine("// </auto-generated>");
                sb.AppendLine("//------------------------------------------------------------------------------");
                sb.AppendLine("#nullable enable");
                sb.AppendLine();
                sb.AppendLine($"using {_config.GetEntityNamespace()};");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("using System.Threading.Tasks;");
                sb.AppendLine("using SqlSugar;");
                sb.AppendLine("using System.Linq;");
                sb.AppendLine("using System;");
                sb.AppendLine("using Aisix.Common.Db;");
                sb.AppendLine("using Aisix.Common.Db.Service;");

                if (NeedsQueryNamespace(customCode))
                {
                    sb.AppendLine($"using {GetQueryNamespace()};");
                }

                foreach (var ns in _config.AdditionalUsings)
                {
                    sb.AppendLine($"using {ns};");
                }

                sb.AppendLine();
                sb.AppendLine($"namespace {_config.GetServiceNamespace()}");
                sb.AppendLine("{");
                sb.Append(classSummary);
                sb.AppendLine($"    public class {className} : BaseService<{tableName}>, I{className}");
                sb.AppendLine("    {");
                sb.AppendLine();
                sb.AppendLine("        #region CustomInterface ");
                sb.Append(customCode);
                sb.AppendLine("        #endregion");
                sb.AppendLine();
                sb.AppendLine("    }");
                sb.AppendLine("}");

                EnsureDirectoryExists(_config.ServiceOutputPath);
                File.WriteAllText(filePath, sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    生成服务实现时出错: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 创建服务接口
        /// </summary>
        private bool CreateIService(string tableName)
        {
            try
            {
                var className = $"{ToCamelCase(tableName)}Service";
                var saveFileName = $"I{className}.cs";
                var filePath = Path.Combine(_config.IServiceOutputPath, saveFileName);

                string customCode = "";
                string classSummary = "";

                if (File.Exists(filePath))
                {
                    var existingContent = File.ReadAllText(filePath);
                    customCode = GetCustomValue(existingContent, "#region CustomInterface \n", "        #endregion");
                    classSummary = GetClassSummary(existingContent, $"I{className}");
                }

                var sb = new StringBuilder();
                sb.AppendLine("//------------------------------------------------------------------------------");
                sb.AppendLine("// <auto-generated>");
                sb.AppendLine("//     此代码已从模板生成，手动更改此文件可能导致应用程序出现意外的行为。");
                sb.AppendLine("//     如果重新生成代码，将覆盖对此文件的手动更改。");
                sb.AppendLine($"//     author {_config.Author}");
                sb.AppendLine("// </auto-generated>");
                sb.AppendLine("//------------------------------------------------------------------------------");
                sb.AppendLine("#nullable enable");
                sb.AppendLine();
                sb.AppendLine($"using {_config.GetEntityNamespace()};");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("using System.Threading.Tasks;");
                sb.AppendLine("using SqlSugar;");
                sb.AppendLine("using System.Linq;");
                sb.AppendLine("using System;");
                sb.AppendLine("using Aisix.Common.Db;");
                sb.AppendLine("using Aisix.Common.Db.Service;");

                if (NeedsQueryNamespace(customCode))
                {
                    sb.AppendLine($"using {GetQueryNamespace()};");
                }

                foreach (var ns in _config.AdditionalUsings)
                {
                    sb.AppendLine($"using {ns};");
                }

                sb.AppendLine();
                sb.AppendLine($"namespace {_config.GetServiceNamespace()}");
                sb.AppendLine("{");
                sb.Append(classSummary);
                sb.AppendLine($"    public interface I{className} : IBaseService<{tableName}>");
                sb.AppendLine("    {");
                sb.AppendLine();
                sb.AppendLine("        #region CustomInterface ");
                sb.Append(customCode);
                sb.AppendLine("        #endregion");
                sb.AppendLine();
                sb.AppendLine("    }");
                sb.AppendLine("}");

                EnsureDirectoryExists(_config.IServiceOutputPath);
                File.WriteAllText(filePath, sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    生成服务接口时出错: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 辅助方法

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private List<string> GetIndexColumns(string tableName, string indexName)
        {
            try
            {
                if (_config.DbType == DataBaseType.PostgreSQL)
                {
                    // PostgreSQL 没有 MySQL 的 information_schema.STATISTICS，索引列需从 pg_catalog 读取。
                    var pgSql = @"
SELECT a.attname
FROM pg_class t
JOIN pg_namespace ns ON ns.oid = t.relnamespace
JOIN pg_index ix ON ix.indrelid = t.oid
JOIN pg_class i ON i.oid = ix.indexrelid
JOIN LATERAL unnest(ix.indkey) WITH ORDINALITY AS cols(attnum, ord) ON true
JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = cols.attnum
WHERE ns.nspname = current_schema()
  AND t.relname = @tableName
  AND i.relname = @indexName
ORDER BY cols.ord";
                    return _db.Ado.SqlQuery<string>(pgSql, new { tableName, indexName }) ?? new List<string>();
                }

                var sql = @"SELECT COLUMN_NAME FROM information_schema.STATISTICS
                           WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName AND INDEX_NAME = @indexName
                           ORDER BY SEQ_IN_INDEX";
                return _db.Ado.SqlQuery<string>(sql, new { tableName, indexName }) ?? new List<string>();
            }
            catch { return new List<string>(); }
        }

        private bool IsUniqueIndex(string tableName, string indexName)
        {
            try
            {
                if (_config.DbType == DataBaseType.PostgreSQL)
                {
                    // PostgreSQL 唯一索引标记位在 pg_index.indisunique。
                    var pgSql = @"
SELECT CASE WHEN ix.indisunique THEN 1 ELSE 0 END
FROM pg_class t
JOIN pg_namespace ns ON ns.oid = t.relnamespace
JOIN pg_index ix ON ix.indrelid = t.oid
JOIN pg_class i ON i.oid = ix.indexrelid
WHERE ns.nspname = current_schema()
  AND t.relname = @tableName
  AND i.relname = @indexName
LIMIT 1";
                    var pgResult = _db.Ado.SqlQuery<int>(pgSql, new { tableName, indexName });
                    return pgResult != null && pgResult.Count > 0 && pgResult[0] == 1;
                }

                var sql = @"SELECT NON_UNIQUE FROM information_schema.STATISTICS
                           WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName AND INDEX_NAME = @indexName LIMIT 1";
                var result = _db.Ado.SqlQuery<int>(sql, new { tableName, indexName });
                return result != null && result.Count > 0 && result[0] == 0;
            }
            catch { return false; }
        }

        private bool IsStringType(string dataType)
        {
            var stringTypes = new[] { "varchar", "char", "nvarchar", "nchar", "text", "ntext", "longtext", "mediumtext" };
            return stringTypes.Any(t => dataType.ToLower().Contains(t));
        }

        private bool NeedsDataTypeAttribute(string dataType)
        {
            var specialTypes = new[] { "text", "ntext", "longtext", "mediumtext", "json", "blob", "longblob" };
            return specialTypes.Any(t => dataType.ToLower().Contains(t));
        }

        private bool IsIdentityColumn(string tableName, DbColumnInfo column)
        {
            if (column.IsIdentity)
            {
                return true;
            }

            if (_config.DbType != DataBaseType.PostgreSQL)
            {
                return false;
            }

            // 对 PostgreSQL 额外读取 information_schema.columns.is_identity，避免回生成丢失 IsIdentity。
            const string sql = @"
SELECT is_identity
FROM information_schema.columns
WHERE table_schema = current_schema()
  AND table_name = @tableName
  AND column_name = @columnName
LIMIT 1;";

            var result = _db.Ado.SqlQuerySingle<string>(sql, new { tableName, columnName = column.DbColumnName });
            return string.Equals(result, "YES", StringComparison.OrdinalIgnoreCase);
        }

        private string GetCSharpType(string dbType, bool isNullable)
        {
            var type = dbType.ToLower();
            string csharpType;

            if (type.Contains("bigint") || type == "int8" || type.Contains("bigserial")) csharpType = "long";
            else if (type.Contains("tinyint")) csharpType = "byte";
            else if (type.Contains("smallint") || type == "int2" || type.Contains("smallserial")) csharpType = "short";
            else if (type.Contains("integer") || type == "int4" || type.Contains("serial") || type.Contains("int")) csharpType = "int";
            else if (type.Contains("decimal") || type.Contains("numeric") || type.Contains("money")) csharpType = "decimal";
            else if (type.Contains("float") || type.Contains("real")) csharpType = "float";
            else if (type.Contains("double")) csharpType = "double";
            else if (type.Contains("bit") || type.Contains("bool")) csharpType = "bool";
            else if (type.Contains("date") || type.Contains("time")) csharpType = "DateTime";
            else if (type.Contains("char") || type.Contains("text")) csharpType = "string";
            else if (type.Contains("binary") || type.Contains("blob")) csharpType = "byte[]";
            else if (type.Contains("uniqueidentifier") || type.Contains("guid") || type.Contains("uuid")) csharpType = "Guid";
            else csharpType = "string";

            if (isNullable && csharpType == "string")
                csharpType = "string?";
            else if (isNullable && csharpType != "byte[]")
                csharpType += "?";

            return csharpType;
        }

        private string? NormalizeDefaultValue(DbColumnInfo column)
        {
            if (string.IsNullOrWhiteSpace(column.DefaultValue))
                return null;

            var defaultValue = column.DefaultValue.Trim();
            // identity 列的 nextval 默认值不应该回写到生成实体中，否则会产出非法 C# 初始化代码。
            if (column.IsIdentity && defaultValue.Contains("nextval", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (_config.DbType == DataBaseType.PostgreSQL)
            {
                // PostgreSQL 默认值常带类型转换后缀，例如 0::numeric、true::boolean，这里统一剥离。
                defaultValue = Regex.Replace(defaultValue, @"::[A-Za-z0-9_\s\[\]\.\""']+", "", RegexOptions.IgnoreCase).Trim();
            }

            defaultValue = defaultValue.Trim();
            while (defaultValue.StartsWith("(") && defaultValue.EndsWith(")") && defaultValue.Length > 2)
            {
                defaultValue = defaultValue[1..^1].Trim();
            }

            if (string.Equals(defaultValue, "NULL", StringComparison.OrdinalIgnoreCase))
                return null;

            if (string.Equals(defaultValue, "now()", StringComparison.OrdinalIgnoreCase))
                return "CURRENT_TIMESTAMP";

            if (defaultValue.StartsWith("'") && defaultValue.EndsWith("'") && defaultValue.Length >= 2)
            {
                defaultValue = defaultValue[1..^1];
            }

            return string.IsNullOrWhiteSpace(defaultValue) ? null : defaultValue;
        }

        private string GetPropertyInitializer(DbColumnInfo column, string? normalizedDefaultValue)
        {
            if (IsStringType(column.DataType) && !column.IsNullable)
                return "string.Empty";

            if (!string.IsNullOrEmpty(normalizedDefaultValue))
            {
                var defaultValue = normalizedDefaultValue.Trim();
                var type = column.DataType.ToLower();

                if (defaultValue.Equals("CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase) && type.Contains("date"))
                    return "DateTime.Now";

                if ((type.Contains("bigint") || type.Contains("smallint") || type.Contains("integer") || type == "int8" || type == "int4" || type == "int2" || type.Contains("int")
                    || type.Contains("decimal") || type.Contains("numeric") || type.Contains("float") || type.Contains("double"))
                    && defaultValue != "NULL")
                {
                    if ((type.Contains("decimal") || type.Contains("numeric")) && !defaultValue.EndsWith("M", StringComparison.OrdinalIgnoreCase))
                        return defaultValue + "M";

                    if ((type.Contains("float") || type.Contains("real")) && !defaultValue.EndsWith("F", StringComparison.OrdinalIgnoreCase))
                        return defaultValue + "F";

                    return defaultValue;
                }

                if (type.Contains("bit") || type.Contains("bool"))
                    return defaultValue == "1" || defaultValue.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ? "true" : "false";

                if (IsStringType(type))
                    return $"\"{defaultValue}\"";
            }

            return "";
        }

        private string GetCustomValue(string content, string start, string end)
        {
            var startPattern = start.Replace("\r\n", "\r?\n");
            var endPattern = end.Replace("\r\n", "\r?\n");
            var r = new Regex("(?<=(" + startPattern + "))[.\\s\\S]*?(?=(" + endPattern + "))");
            return r.Match(content).Value;
        }

        private string GetClassSummary(string fileContent, string className)
        {
            if (string.IsNullOrEmpty(fileContent)) return "";

            try
            {
                var pattern = @"((?:[ \t]*///[^\r\n]*[\r\n]+)+)(?:[ \t]*\[[^\]]+\][\r\n]+)*[ \t]*public\s+(?:class|interface)\s+" + Regex.Escape(className);
                var match = Regex.Match(fileContent, pattern);
                return match.Success ? match.Groups[1].Value : "";
            }
            catch { return ""; }
        }

        private bool NeedsQueryNamespace(string customCode)
        {
            if (string.IsNullOrWhiteSpace(customCode))
                return false;

            return Regex.IsMatch(customCode, @"\b[A-Za-z_][A-Za-z0-9_]*Query\b");
        }

        private string GetQueryNamespace()
        {
            return $"{_config.SolutionName}.Interface.Model.Query";
        }

        private static string ToCamelCase(string value, bool upperAtFirst = true)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var result = string.Empty;
            var toUpper = upperAtFirst;

            foreach (var c in value)
            {
                if (c == '_') { toUpper = true; continue; }
                result += toUpper ? char.ToUpper(c) : c;
                toUpper = false;
            }

            return result;
        }

        #endregion
    }
}
