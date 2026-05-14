using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Aisix.CodeFirst.Dialects;
using Aisix.CodeFirst.Extensions;
using Aisix.Common.Db;
using SqlSugar;

namespace Aisix.CodeFirst.Services
{
    public class CodeFirstService : ICodeFirstService
    {
        private readonly CodeFirstConfiguration _configuration;
        private readonly Dictionary<string, EnvironmentSettings> _environments;

        private ISqlSugarClient? _db;
        private IDatabaseDialect? _dialect;
        private EnvironmentSettings? _currentEnv;
        private string _currentEnvName = string.Empty;

        // 实体分类缓存
        private List<Type> _normalEntities = new();
        private List<Type> _splitEntities = new();

        // XML 文档缓存：Key = 程序集名, Value = XML 文档
        private Dictionary<string, XmlDocument> _xmlDocs = new();

        public CodeFirstService(CodeFirstConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _environments = configuration.Environments;
        }

        public void Run()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            PrintHeader();

            // 1. 选择环境
            if (!SelectEnvironment())
            {
                return;
            }

            // 2. 加载实体
            LoadEntities();

            // 3. 主菜单循环
            MainMenuLoop();
        }

        #region 环境选择

        private void PrintHeader()
        {
            Console.Clear();
            Console.WriteLine("============================================================");
            Console.WriteLine("  OpenRTBAdx CodeFirst 工具 - 实体驱动表结构更新");
            Console.WriteLine("============================================================");
            Console.WriteLine();
        }

        private bool SelectEnvironment()
        {
            Console.WriteLine("请选择目标环境：");
            Console.WriteLine();

            var envList = _environments.ToList();
            for (int i = 0; i < envList.Count; i++)
            {
                var env = envList[i];
                var warning = env.Value.RequireConfirmation ? " [!]" : "";
                Console.WriteLine($"  [{i + 1}] {env.Key,-15} {env.Value.Description}{warning}");
            }

            Console.WriteLine();
            Console.Write("请输入选择: ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input) || !int.TryParse(input, out int index) || index < 1 || index > envList.Count)
            {
                PrintError("无效的选择");
                return false;
            }

            var selected = envList[index - 1];
            _currentEnvName = selected.Key;
            _currentEnv = selected.Value;

            // 生产环境确认
            if (_currentEnv.RequireConfirmation)
            {
                Console.WriteLine();
                PrintWarning($"您正在操作【{_currentEnv.Description}】数据库！");

                // 解析连接字符串显示服务器信息
                var serverInfo = ParseConnectionString(_currentEnv.ConnectionString);
                Console.WriteLine($"  服务器: {serverInfo}");
                Console.WriteLine();

                var confirmCode = GenerateConfirmCode(_currentEnv.ConfirmationPrefix);
                Console.Write($"请输入确认码 [{confirmCode}] 继续: ");
                var inputCode = Console.ReadLine()?.Trim();

                if (inputCode != confirmCode)
                {
                    PrintError("确认码错误，操作已取消");
                    return false;
                }
            }

            // 创建数据库连接
            try
            {
                _db = SqlSugarExtensions.CreateSqlSugarClient(_currentEnv);
                _dialect = DatabaseDialectFactory.Create(_currentEnv.DbType);
                PrintSuccess($"已连接到 {_currentEnv.Description}");
                Console.WriteLine();
                return true;
            }
            catch (Exception ex)
            {
                PrintError($"连接数据库失败: {ex.Message}");
                return false;
            }
        }

        private string ParseConnectionString(string connectionString)
        {
            var parts = connectionString.Split(';')
                .Select(p => p.Split('='))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim().ToLower(), p => p[1].Trim());

            var server = parts.GetValueOrDefault("server", "unknown");
            var port = parts.GetValueOrDefault("port", "3306");
            var database = parts.GetValueOrDefault("database", "unknown");

            return $"{server}:{port}/{database}";
        }

        private string GenerateConfirmCode(string prefix)
        {
            var random = new Random();
            return $"{prefix}-{random.Next(1000, 9999)}";
        }

        #endregion

        #region 实体加载

        private void LoadEntities()
        {
            try
            {
                var assembly = Assembly.Load(_configuration.EntityAssembly);
                var allTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && t.Namespace == _configuration.EntityNamespace)
                    .Where(t => t.GetCustomAttribute<SugarTable>() != null || t.Name.Contains("_"))
                    .ToList();

                _splitEntities = allTypes.Where(t => t.GetCustomAttribute<SplitTableAttribute>() != null).ToList();
                _normalEntities = allTypes.Where(t => t.GetCustomAttribute<SplitTableAttribute>() == null).ToList();

                // 加载 XML 文档
                LoadXmlDocumentation(assembly);

                Console.WriteLine($"发现 {allTypes.Count} 个实体类：");
                Console.WriteLine($"  [普通表] 共 {_normalEntities.Count} 个");
                Console.WriteLine($"  [分表模板] 共 {_splitEntities.Count} 个 (标记 [SplitTable])");
                if (_xmlDocs.Count > 0)
                {
                    Console.WriteLine($"  [XML文档] 已加载 {_xmlDocs.Count} 个程序集的文档 (将自动提取 Summary 作为字段描述)");
                }
                Console.WriteLine();
                Console.WriteLine("注：分表模板也可在「更新普通表结构」中更新其基础表");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                PrintError($"加载实体失败: {ex.Message}");
            }
        }

        #region XML 文档加载

        /// <summary>
        /// 加载程序集的 XML 文档
        /// </summary>
        private void LoadXmlDocumentation(Assembly assembly)
        {
            try
            {
                var assemblyName = assembly.GetName().Name;
                if (string.IsNullOrEmpty(assemblyName)) return;

                // 尝试从多个位置加载 XML 文件
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var xmlPaths = new[]
                {
                    Path.Combine(baseDir, $"{assemblyName}.xml"),
                    Path.Combine(baseDir, "bin", $"{assemblyName}.xml"),
                    Path.Combine(baseDir, "..", $"{assemblyName}.xml"),
                    Path.Combine(baseDir, "..", "..", $"{assemblyName}.xml"),
                    Path.Combine(baseDir, "..", "..", "..", $"{assemblyName}.xml"),
                    Path.Combine(baseDir, "..", "..", "..", "..", $"{assemblyName}.xml"),
                };

                foreach (var xmlPath in xmlPaths)
                {
                    var fullPath = Path.GetFullPath(xmlPath);
                    if (File.Exists(fullPath))
                    {
                        var xmlDoc = new XmlDocument();
                        xmlDoc.Load(fullPath);
                        _xmlDocs[assemblyName] = xmlDoc;
                        break;
                    }
                }
            }
            catch
            {
                // XML 文档加载失败不影响主流程
            }
        }

        /// <summary>
        /// 获取属性的 Summary 注释
        /// </summary>
        private string? GetPropertySummary(PropertyInfo property)
        {
            try
            {
                // 查找程序集的 XML 文档
                var assembly = property.DeclaringType?.Assembly;
                var assemblyName = assembly?.GetName().Name;

                if (assemblyName == null || !_xmlDocs.TryGetValue(assemblyName, out var xmlDoc))
                {
                    return null;
                }

                // 构建 XPath：//member[@name="P:命名空间.类名.属性名"]
                var typeName = property.DeclaringType?.FullName;
                if (string.IsNullOrEmpty(typeName)) return null;

                var memberName = $"P:{typeName}.{property.Name}";
                var summaryNode = xmlDoc.SelectSingleNode($"//member[@name='{memberName}']/summary");

                if (summaryNode != null)
                {
                    // 清理 Summary 内容：去除首尾空白和换行
                    var summary = summaryNode.InnerText.Trim();
                    return string.IsNullOrEmpty(summary) ? null : summary;
                }
            }
            catch
            {
                // 读取失败返回 null
            }

            return null;
        }

        /// <summary>
        /// 获取类的 Summary 注释
        /// </summary>
        private string? GetClassSummary(Type type)
        {
            try
            {
                var assemblyName = type.Assembly.GetName().Name;
                if (assemblyName == null || !_xmlDocs.TryGetValue(assemblyName, out var xmlDoc))
                {
                    return null;
                }

                var typeName = type.FullName;
                if (string.IsNullOrEmpty(typeName)) return null;

                var memberName = $"T:{typeName}";
                var summaryNode = xmlDoc.SelectSingleNode($"//member[@name='{memberName}']/summary");

                if (summaryNode != null)
                {
                    var summary = summaryNode.InnerText.Trim();
                    return string.IsNullOrEmpty(summary) ? null : summary;
                }
            }
            catch
            {
                // 读取失败返回 null
            }

            return null;
        }

        #endregion

        #endregion

        #region 主菜单

        private void MainMenuLoop()
        {
            while (true)
            {
                Console.WriteLine("请选择操作：");
                Console.WriteLine("  [1] 更新普通表结构");
                Console.WriteLine("  [2] 更新分表结构");
                Console.WriteLine("  [3] 更新全部表结构");
                Console.WriteLine("  [4] 查看表结构差异（不执行）");
                Console.WriteLine("  [5] 生成迁移 SQL 脚本（按字段顺序）");
                Console.WriteLine("  [q] 退出");
                Console.WriteLine();
                Console.Write("请输入选择: ");

                var input = Console.ReadLine()?.Trim().ToLower();

                Console.WriteLine();

                switch (input)
                {
                    case "1":
                        UpdateNormalTables();
                        break;
                    case "2":
                        UpdateSplitTables();
                        break;
                    case "3":
                        UpdateAllTables();
                        break;
                    case "4":
                        ViewDifferences();
                        break;
                    case "5":
                        GenerateMigrationSql();
                        break;
                    case "q":
                    case "quit":
                        PrintInfo("感谢使用 OpenRTBAdx CodeFirst 工具，再见！");
                        return;
                    default:
                        PrintError("无效的选择，请重新输入");
                        break;
                }

                Console.WriteLine();
                Console.WriteLine("按任意键继续...");
                Console.ReadKey(true);
                Console.Clear();
                PrintHeader();
                Console.WriteLine($"当前环境: {_currentEnv?.Description} ({_currentEnvName})");
                Console.WriteLine($"实体统计: 普通表 {_normalEntities.Count} 个, 分表 {_splitEntities.Count} 个");
                Console.WriteLine();
            }
        }

        #endregion

        #region 普通表更新

        private void UpdateNormalTables()
        {
            // 合并普通表和分表模板（分表模板作为基础表更新）
            var allEntities = _normalEntities.Concat(_splitEntities).ToList();

            if (allEntities.Count == 0)
            {
                PrintWarning("没有发现实体");
                return;
            }

            Console.WriteLine("=== 表实体列表 ===");
            Console.WriteLine();

            for (int i = 0; i < allEntities.Count; i++)
            {
                var entity = allEntities[i];
                var tableName = GetTableName(entity);
                var description = GetTableDescription(entity);
                var isSplit = _splitEntities.Contains(entity);
                var suffix = isSplit ? " [分表模板]" : "";
                Console.WriteLine($"  [{i + 1,2}] {tableName,-35} {description}{suffix}");
            }

            Console.WriteLine();
            Console.WriteLine("请选择要更新的表：");
            Console.WriteLine("  - 输入编号（如：1）更新单个表");
            Console.WriteLine("  - 输入多个编号（如：1,3,5）更新多张表");
            Console.WriteLine("  - 输入 'all' 或 'a' 更新全部表");
            Console.WriteLine("  - 输入 'b' 返回上级菜单");
            Console.WriteLine();
            Console.Write("请输入选择: ");

            var input = Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrEmpty(input) || input == "b")
            {
                return;
            }

            List<Type> selectedEntities;

            if (input == "all" || input == "a")
            {
                selectedEntities = allEntities;
            }
            else
            {
                var indices = ParseIndices(input, allEntities.Count);
                if (indices.Count == 0)
                {
                    PrintError("未选择有效的表");
                    return;
                }
                selectedEntities = indices.Select(i => allEntities[i - 1]).ToList();
            }

            Console.WriteLine();
            Console.WriteLine($"将更新以下 {selectedEntities.Count} 张表：");
            foreach (var entity in selectedEntities)
            {
                Console.WriteLine($"  - {GetTableName(entity)}");
            }

            // 显示表结构差异
            Console.WriteLine();
            Console.WriteLine("正在分析表结构差异...");
            Console.WriteLine();

            var entitiesToUpdate = new List<Type>();
            foreach (var entity in selectedEntities)
            {
                var tableName = GetTableName(entity);
                var differences = GetTableDifferences(entity);

                if (differences.Count == 0)
                {
                    Console.WriteLine($"[{tableName}] 无差异");
                }
                else
                {
                    Console.WriteLine($"[{tableName}] 检测到 {differences.Count} 处变更：");
                    foreach (var diff in differences)
                    {
                        if (diff.StartsWith("+"))
                            PrintSuccess($"    {diff}");
                        else if (diff.StartsWith("-"))
                            PrintError($"    {diff}");
                        else
                            PrintWarning($"    {diff}");
                    }
                    entitiesToUpdate.Add(entity);
                }
            }

            if (entitiesToUpdate.Count == 0)
            {
                Console.WriteLine();
                PrintInfo("没有需要更新的表");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"共 {entitiesToUpdate.Count} 张表需要更新");

            // 生产环境需要再次确认
            if (!ConfirmExecution())
            {
                return;
            }

            // 执行更新
            ExecuteNormalTableUpdate(entitiesToUpdate);
        }

        private void ExecuteNormalTableUpdate(List<Type> entities)
        {
            Console.WriteLine();
            Console.WriteLine("正在更新表结构...");
            Console.WriteLine();

            int success = 0, failed = 0;
            var logEntries = new List<string>();

            foreach (var entity in entities)
            {
                var tableName = GetTableName(entity);
                try
                {
                    // 检查差异
                    var differences = GetTableDifferences(entity);

                    if (differences.Count == 0)
                    {
                        Console.WriteLine($"  - {tableName} 无需更新");
                        continue;
                    }

                    // 检查是否有删除操作
                    var hasDelete = differences.Any(d => d.StartsWith("-"));
                    if (hasDelete && !_currentEnv!.AllowDeleteColumn)
                    {
                        PrintWarning($"  ! {tableName} 包含删除字段操作，已跳过（生产环境禁止删除）");
                        logEntries.Add($"[SKIP] {tableName} - 包含删除操作");
                        continue;
                    }

                    var tableExists = IsTableExists(tableName);

                    // PostgreSQL 跨类型家族变更经常需要 USING 显式转换。
                    // 但如果表是空的，可以直接放行，让数据库在无数据场景下完成改列。
                    var blockingTypeChanges = GetBlockingPostgresTypeChanges(entity, tableName);
                    if (blockingTypeChanges.Count > 0)
                    {
                        var reason = string.Join("；", blockingTypeChanges);
                        PrintError($"  x {tableName} 更新已阻止: {reason}");
                        failed++;
                        logEntries.Add($"[BLOCK] {tableName} - {reason}");
                        continue;
                    }

                    if (!tableExists)
                    {
                        ExecuteCreateTable(entity, tableName);
                        SyncTableComment(tableName, entity);
                        CreateIndexesFromEntity(tableName, entity);
                        SyncColumnComments(tableName, entity);
                        RepairPostgresIdentityDefaults(tableName, entity);

                        PrintSuccess($"  + {tableName} 创建成功");
                        success++;
                        logEntries.Add($"[OK] {tableName} (create-table)");
                        continue;
                    }

                    // PostgreSQL 现有表优先走我们自己的迁移 SQL，避免 SqlSugar 在已存在主键的表上重复补主键。
                    // 这里统一覆盖空表/非空表，删除列仍由 AllowDeleteColumn 控制。
                    if (TryApplyPostgresManagedMigration(entity, tableName))
                    {
                        SyncTableComment(tableName, entity);
                        CreateIndexesFromEntity(tableName, entity);
                        SyncColumnComments(tableName, entity);
                        RepairPostgresIdentityDefaults(tableName, entity);

                        PrintSuccess($"  + {tableName} 更新成功");
                        success++;
                        logEntries.Add($"[OK] {tableName} (postgres-managed migration)");
                        continue;
                    }

                    if (IsCommentOnlyDifferences(differences))
                    {
                        SyncTableComment(tableName, entity);
                        SyncColumnComments(tableName, entity);
                        PrintSuccess($"  + {tableName} 更新成功");
                        success++;
                        logEntries.Add($"[OK] {tableName} (comment-only)");
                        continue;
                    }

                    // 执行更新
                    var isSplitTemplate = _splitEntities.Contains(entity);
                    if (isSplitTemplate)
                    {
                        // 分表模板需要指定基础表名（不带日期后缀）
                        // 使用 SetStringDefaultToStringEmpty 配合 MappingTables 指定表名
                        _db!.MappingTables.Add(entity.Name, tableName);
                        _db!.CodeFirst.InitTables(entity);
                        _db!.MappingTables.RemoveAll(m => m.EntityName == entity.Name);
                    }
                    else
                    {
                        _db!.CodeFirst.InitTables(entity);
                    }

                    SyncTableComment(tableName, entity);

                    // 创建索引（从实体特性中获取所有索引定义）
                    CreateIndexesFromEntity(tableName, entity);

                    // 同步字段注释（从 ColumnDescription 或 Summary）
                    SyncColumnComments(tableName, entity);

                    // PostgreSQL: 修复已存在表的 identity/sequence 默认值
                    RepairPostgresIdentityDefaults(tableName, entity);

                    PrintSuccess($"  + {tableName} 更新成功");
                    success++;
                    logEntries.Add($"[OK] {tableName}");
                }
                catch (Exception ex)
                {
                    PrintError($"  x {tableName} 更新失败: {ex.Message}");
                    failed++;
                    logEntries.Add($"[FAIL] {tableName} - {ex.Message}");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"更新完成！成功: {success}, 失败: {failed}");

            // 写入日志
            WriteLog("NormalTable", logEntries);
        }

        #endregion

        #region 分表更新

        private void UpdateSplitTables()
        {
            if (_splitEntities.Count == 0)
            {
                PrintWarning("没有发现分表实体");
                return;
            }

            Console.WriteLine("=== 分表实体列表 ===");
            Console.WriteLine();

            for (int i = 0; i < _splitEntities.Count; i++)
            {
                var entity = _splitEntities[i];
                var tableName = GetTableName(entity);
                var splitTableCount = GetSplitTableCount(entity);
                Console.WriteLine($"  [{i + 1}] {tableName,-35} (已存在 {splitTableCount} 张分表)");
            }

            Console.WriteLine();
            Console.WriteLine("请选择要更新的分表：");
            Console.WriteLine("  - 输入编号（如：1）更新单个分表");
            Console.WriteLine("  - 输入多个编号（如：1,3）更新多个分表");
            Console.WriteLine("  - 输入 'all' 或 'a' 更新全部分表");
            Console.WriteLine("  - 输入 'b' 返回上级菜单");
            Console.WriteLine();
            Console.Write("请输入选择: ");

            var input = Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrEmpty(input) || input == "b")
            {
                return;
            }

            List<Type> selectedEntities;

            if (input == "all" || input == "a")
            {
                selectedEntities = _splitEntities;
            }
            else
            {
                var indices = ParseIndices(input, _splitEntities.Count);
                if (indices.Count == 0)
                {
                    PrintError("未选择有效的分表");
                    return;
                }
                selectedEntities = indices.Select(i => _splitEntities[i - 1]).ToList();
            }

            Console.WriteLine();
            Console.WriteLine($"将更新以下 {selectedEntities.Count} 个分表实体的所有分表：");
            foreach (var entity in selectedEntities)
            {
                var count = GetSplitTableCount(entity);
                Console.WriteLine($"  - {GetTableName(entity)} ({count} 张分表)");
            }

            // 显示表结构差异（用第一张分表来分析）
            Console.WriteLine();
            Console.WriteLine("正在分析表结构差异...");
            Console.WriteLine();

            var entitiesToUpdate = new List<Type>();
            foreach (var entity in selectedEntities)
            {
                var tableName = GetTableName(entity);
                var splitTables = GetSplitTableNames(entity);

                if (splitTables.Count == 0)
                {
                    Console.WriteLine($"[{tableName}] 暂无分表");
                    continue;
                }

                var differences = GetTableDifferencesForSplit(entity, splitTables.First());

                if (differences.Count == 0)
                {
                    Console.WriteLine($"[{tableName}] 无差异 ({splitTables.Count} 张分表)");
                }
                else
                {
                    Console.WriteLine($"[{tableName}] 检测到 {differences.Count} 处变更 ({splitTables.Count} 张分表)：");
                    foreach (var diff in differences)
                    {
                        if (diff.StartsWith("+"))
                            PrintSuccess($"    {diff}");
                        else if (diff.StartsWith("-"))
                            PrintError($"    {diff}");
                        else
                            PrintWarning($"    {diff}");
                    }
                    entitiesToUpdate.Add(entity);
                }
            }

            if (entitiesToUpdate.Count == 0)
            {
                Console.WriteLine();
                PrintInfo("没有需要更新的分表");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"共 {entitiesToUpdate.Count} 个分表实体需要更新");

            // 生产环境需要再次确认
            if (!ConfirmExecution())
            {
                return;
            }

            // 执行更新
            ExecuteSplitTableUpdate(entitiesToUpdate);
        }

        private void ExecuteSplitTableUpdate(List<Type> entities)
        {
            Console.WriteLine();
            Console.WriteLine("正在更新分表结构...");
            Console.WriteLine();

            int success = 0, failed = 0;
            var logEntries = new List<string>();

            foreach (var entity in entities)
            {
                var tableName = GetTableName(entity);
                try
                {
                    // 获取所有分表
                    var splitTables = GetSplitTableNames(entity);
                    var sampleTable = splitTables.FirstOrDefault();

                    if (!string.IsNullOrEmpty(sampleTable))
                    {
                        var differences = GetTableDifferencesForSplit(entity, sampleTable);
                        if (IsCommentOnlyDifferences(differences))
                        {
                            foreach (var splitTable in splitTables)
                            {
                                SyncTableComment(splitTable, entity);
                                SyncColumnComments(splitTable, entity);
                            }

                            PrintSuccess($"  + {tableName} 所有分表注释同步成功");
                            success++;
                            logEntries.Add($"[OK] {tableName} - {splitTables.Count} 张分表 (comment-only)");
                            continue;
                        }
                    }

                    Console.WriteLine($"  [{tableName}] 正在更新 {splitTables.Count} 张分表...");

                    // 使用 SplitTables().InitTables 更新所有分表
                    _db!.CodeFirst.SplitTables().InitTables(entity);

                    foreach (var splitTable in splitTables)
                    {
                        SyncTableComment(splitTable, entity);
                        SyncColumnComments(splitTable, entity);
                    }

                    PrintSuccess($"  + {tableName} 所有分表更新成功");
                    success++;
                    logEntries.Add($"[OK] {tableName} - {splitTables.Count} 张分表");
                }
                catch (Exception ex)
                {
                    PrintError($"  x {tableName} 更新失败: {ex.Message}");
                    failed++;
                    logEntries.Add($"[FAIL] {tableName} - {ex.Message}");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"更新完成！成功: {success}, 失败: {failed}");

            // 写入日志
            WriteLog("SplitTable", logEntries);
        }

        #endregion

        #region 更新全部

        private void UpdateAllTables()
        {
            // 合并所有实体（普通表 + 分表模板）
            var allEntities = _normalEntities.Concat(_splitEntities).ToList();

            Console.WriteLine("将逐张检查并更新所有表结构：");
            Console.WriteLine($"  - 普通表: {_normalEntities.Count} 张");
            Console.WriteLine($"  - 分表模板: {_splitEntities.Count} 个");
            Console.WriteLine($"  - 合计: {allEntities.Count} 张");
            Console.WriteLine();

            // 生产环境需要先确认
            if (_currentEnvName == "Production")
            {
                if (!ConfirmExecution())
                {
                    return;
                }
            }

            Console.WriteLine("正在分析表结构差异...");
            Console.WriteLine();

            int success = 0, failed = 0, skipped = 0;
            var logEntries = new List<string>();

            foreach (var entity in allEntities)
            {
                var tableName = GetTableName(entity);
                var isSplitTemplate = _splitEntities.Contains(entity);
                var typeTag = isSplitTemplate ? "[分表模板]" : "[普通表]";

                try
                {
                    // 检查差异
                    var differences = GetTableDifferences(entity);

                    if (differences.Count == 0)
                    {
                        Console.WriteLine($"{typeTag} {tableName} - 无差异，跳过");
                        continue;
                    }

                    // 显示差异
                    Console.WriteLine();
                    Console.WriteLine($"{typeTag} {tableName} 检测到 {differences.Count} 处变更：");
                    foreach (var diff in differences)
                    {
                        if (diff.StartsWith("+"))
                            PrintSuccess($"    {diff}");
                        else if (diff.StartsWith("-"))
                            PrintError($"    {diff}");
                        else
                            Console.WriteLine($"    {diff}");
                    }

                    // 检查是否有删除操作
                    var hasDelete = differences.Any(d => d.StartsWith("-"));
                    if (hasDelete && !_currentEnv!.AllowDeleteColumn)
                    {
                        PrintWarning($"  ! 包含删除字段操作，已跳过（生产环境禁止删除）");
                        logEntries.Add($"[SKIP] {tableName} - 包含删除操作");
                        skipped++;
                        continue;
                    }

                    // 逐张表确认
                    Console.WriteLine();
                    Console.Write($"是否更新此表？(y=更新 / n=跳过 / q=退出): ");
                    var input = Console.ReadLine()?.Trim().ToLower();

                    if (input == "q")
                    {
                        Console.WriteLine("已取消后续更新");
                        break;
                    }

                    if (input != "y")
                    {
                        Console.WriteLine($"  - {tableName} 已跳过");
                        skipped++;
                        logEntries.Add($"[SKIP] {tableName} - 用户跳过");
                        continue;
                    }

                    if (IsCommentOnlyDifferences(differences))
                    {
                        SyncTableComment(tableName, entity);
                        SyncColumnComments(tableName, entity);

                        PrintSuccess($"  + {tableName} 更新成功");
                        success++;
                        logEntries.Add($"[OK] {tableName} (comment-only)");
                        continue;
                    }

                    // 执行更新
                    if (isSplitTemplate)
                    {
                        _db!.MappingTables.Add(entity.Name, tableName);
                        _db!.CodeFirst.InitTables(entity);
                        _db!.MappingTables.RemoveAll(m => m.EntityName == entity.Name);
                    }
                    else
                    {
                        _db!.CodeFirst.InitTables(entity);
                    }

                    SyncTableComment(tableName, entity);
                    SyncColumnComments(tableName, entity);

                    PrintSuccess($"  + {tableName} 更新成功");
                    success++;
                    logEntries.Add($"[OK] {tableName}");
                }
                catch (Exception ex)
                {
                    PrintError($"  x {tableName} 更新失败: {ex.Message}");
                    failed++;
                    logEntries.Add($"[FAIL] {tableName} - {ex.Message}");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"更新完成！成功: {success}, 失败: {failed}, 跳过: {skipped}");

            // 写入日志
            WriteLog("UpdateAll", logEntries);
        }

        #endregion

        #region 生成迁移 SQL

        private void GenerateMigrationSql()
        {
            Console.WriteLine("请选择要生成迁移 SQL 的表类型：");
            Console.WriteLine("  [1] 普通表");
            Console.WriteLine("  [2] 分表");
            Console.WriteLine("  [b] 返回");
            Console.WriteLine();
            Console.Write("请输入选择: ");

            var typeInput = Console.ReadLine()?.Trim().ToLower();

            List<Type> entities;
            bool isSplitTable = false;
            switch (typeInput)
            {
                case "1":
                    entities = _normalEntities;
                    break;
                case "2":
                    entities = _splitEntities;
                    isSplitTable = true;
                    break;
                default:
                    return;
            }

            if (entities.Count == 0)
            {
                PrintWarning("没有发现相关实体");
                return;
            }

            // 显示实体列表
            Console.WriteLine();
            Console.WriteLine(isSplitTable ? "=== 分表实体列表 ===" : "=== 普通表实体列表 ===");
            Console.WriteLine();

            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                var tableName = GetTableName(entity);
                if (isSplitTable)
                {
                    var count = GetSplitTableCount(entity);
                    Console.WriteLine($"  [{i + 1}] {tableName,-35} ({count} 张分表)");
                }
                else
                {
                    Console.WriteLine($"  [{i + 1}] {tableName}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("请选择要生成 SQL 的表：");
            Console.WriteLine("  - 输入编号（如：1）或多个编号（如：1,3）");
            Console.WriteLine("  - 输入 'all' 或 'a' 生成全部");
            Console.WriteLine("  - 输入 'b' 返回");
            Console.WriteLine();
            Console.Write("请输入选择: ");

            var input = Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrEmpty(input) || input == "b")
            {
                return;
            }

            List<Type> selectedEntities;
            if (input == "all" || input == "a")
            {
                selectedEntities = entities;
            }
            else
            {
                var indices = ParseIndices(input, entities.Count);
                if (indices.Count == 0)
                {
                    PrintError("未选择有效的表");
                    return;
                }
                selectedEntities = indices.Select(i => entities[i - 1]).ToList();
            }

            // 生成 SQL
            var allSql = new List<string>();
            allSql.Add("-- ============================================================");
            allSql.Add($"-- OpenRTBAdx 迁移脚本");
            allSql.Add($"-- 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            allSql.Add($"-- 目标环境: {_currentEnvName} ({_currentEnv?.Description})");
            allSql.Add("-- ============================================================");
            allSql.Add("");

            int totalChanges = 0;

            foreach (var entity in selectedEntities)
            {
                var tableName = GetTableName(entity);
                var entitySql = GenerateEntityMigrationSql(entity, isSplitTable);

                if (entitySql.Count > 0)
                {
                    allSql.Add($"-- ------------------------------------------------------------");
                    allSql.Add($"-- 表: {tableName}");
                    allSql.Add($"-- ------------------------------------------------------------");
                    allSql.AddRange(entitySql);
                    allSql.Add("");
                    totalChanges += entitySql.Count(s => s.StartsWith("ALTER"));
                }
            }

            if (totalChanges == 0)
            {
                PrintInfo("没有检测到需要迁移的变更");
                return;
            }

            // 显示 SQL 预览
            Console.WriteLine();
            Console.WriteLine("=== 生成的迁移 SQL ===");
            Console.WriteLine();
            foreach (var line in allSql)
            {
                if (line.StartsWith("--"))
                    PrintInfo(line);
                else if (line.StartsWith("ALTER"))
                    PrintWarning(line);
                else
                    Console.WriteLine(line);
            }

            // 询问是否保存到文件
            Console.WriteLine();
            Console.Write("是否保存到文件？(y/n): ");
            var saveInput = Console.ReadLine()?.Trim().ToLower();

            if (saveInput == "y" || saveInput == "yes")
            {
                SaveMigrationSql(allSql);
            }

            // 询问是否执行
            Console.WriteLine();
            Console.Write("是否立即执行这些 SQL？(y/n): ");
            var execInput = Console.ReadLine()?.Trim().ToLower();

            if (execInput == "y" || execInput == "yes")
            {
                if (_currentEnv!.RequireConfirmation)
                {
                    var confirmCode = GenerateConfirmCode(_currentEnv.ConfirmationPrefix);
                    Console.Write($"请输入确认码 [{confirmCode}] 执行: ");
                    var inputCode = Console.ReadLine()?.Trim();
                    if (inputCode != confirmCode)
                    {
                        PrintError("确认码错误，操作已取消");
                        return;
                    }
                }

                ExecuteMigrationSql(allSql);
            }
        }

        private List<string> GenerateEntityMigrationSql(Type entityType, bool isSplitTable)
        {
            var sqlList = new List<string>();

            try
            {
                // 获取要更新的表名列表
                List<string> tableNames;
                if (isSplitTable)
                {
                    tableNames = GetSplitTableNames(entityType);
                }
                else
                {
                    tableNames = new List<string> { GetTableName(entityType) };
                }

                if (tableNames.Count == 0)
                {
                    return sqlList;
                }

                // 用第一张表来分析差异
                var sampleTable = tableNames.First();
                if (!_db!.DbMaintenance.IsAnyTable(sampleTable, false))
                {
                    sqlList.Add($"-- 表 {sampleTable} 不存在，请先创建表");
                    return sqlList;
                }

                // 获取数据库中的列（按顺序）
                var dbColumns = _db.DbMaintenance.GetColumnInfosByTableName(sampleTable, false);
                var dbColumnDict = dbColumns.ToDictionary(c => c.DbColumnName.ToLower(), c => c);
                var dbColumnOrder = dbColumns.Select(c => c.DbColumnName.ToLower()).ToList();

                // 获取实体中的属性（按定义顺序）
                var properties = entityType.GetProperties()
                    .Where(p => p.GetCustomAttribute<SugarColumn>()?.IsIgnore != true)
                    .ToList();

                // 找出新增和变更的字段
                string? previousColumn = null;
                foreach (var prop in properties)
                {
                    var columnAttr = prop.GetCustomAttribute<SugarColumn>();
                    var columnName = columnAttr?.ColumnName ?? prop.Name;

                    if (!dbColumnDict.ContainsKey(columnName.ToLower()))
                    {
                        foreach (var tableName in tableNames)
                        {
                            sqlList.AddRange(GenerateAddColumnMigrationSql(tableName, prop, columnAttr, previousColumn));
                        }
                    }
                    else
                    {
                        // 已有字段，检查是否需要修改
                        var dbColumn = dbColumnDict[columnName.ToLower()];
                        var columnDiffs = CompareColumnAttributes(prop, columnAttr, dbColumn);
                        if (columnDiffs.Count > 0)
                        {
                            foreach (var tableName in tableNames)
                            {
                                sqlList.AddRange(GenerateAlterColumnMigrationSql(tableName, prop, columnAttr, dbColumn));
                            }
                        }
                    }

                    previousColumn = columnName;
                }

                // 检查是否有字段需要删除
                var entityColumnNames = properties
                    .Select(p => (p.GetCustomAttribute<SugarColumn>()?.ColumnName ?? p.Name).ToLower())
                    .ToHashSet();

                foreach (var dbColumn in dbColumns)
                {
                    if (!entityColumnNames.Contains(dbColumn.DbColumnName.ToLower()))
                    {
                        foreach (var tableName in tableNames)
                        {
                            sqlList.Add($"-- [危险] ALTER TABLE {QuoteIdentifier(tableName)} DROP COLUMN {QuoteIdentifier(dbColumn.DbColumnName)};");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sqlList.Add($"-- 生成失败: {ex.Message}");
            }

            return sqlList;
        }

        private List<string> GenerateAddColumnMigrationSql(string tableName, PropertyInfo prop, SugarColumn? columnAttr, string? previousColumn)
        {
            // PostgreSQL 不支持 MySQL 的 AFTER/FIRST 语法，按方言拆分生成。
            if (_currentEnv?.DbType == DataBaseType.PostgreSQL)
            {
                return GeneratePostgresAddColumnMigrationSql(tableName, prop, columnAttr);
            }

            var columnDef = GenerateColumnDefinition(prop, columnAttr);
            var afterClause = previousColumn != null ? $" AFTER {QuoteIdentifier(previousColumn)}" : " FIRST";
            return new List<string> { $"ALTER TABLE {QuoteIdentifier(tableName)} ADD COLUMN {columnDef}{afterClause};" };
        }

        private List<string> GenerateAlterColumnMigrationSql(string tableName, PropertyInfo prop, SugarColumn? columnAttr, DbColumnInfo dbColumn)
        {
            // PostgreSQL 需要将列变更拆成 TYPE / NULLABLE / COMMENT 多条语句。
            if (_currentEnv?.DbType == DataBaseType.PostgreSQL)
            {
                return GeneratePostgresAlterColumnMigrationSql(tableName, prop, columnAttr, dbColumn);
            }

            var columnDef = GenerateColumnDefinition(prop, columnAttr);
            return new List<string> { $"ALTER TABLE {QuoteIdentifier(tableName)} MODIFY COLUMN {columnDef};" };
        }

        private string GenerateColumnDefinition(PropertyInfo prop, SugarColumn? columnAttr)
        {
            var columnName = columnAttr?.ColumnName ?? prop.Name;
            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var isNullable = columnAttr?.IsNullable ?? Nullable.GetUnderlyingType(prop.PropertyType) != null;

            var dataType = ResolveColumnDataType(propType, columnAttr);

            // 构建列定义
            var sb = new System.Text.StringBuilder();
            sb.Append($"{QuoteIdentifier(columnName)} {dataType}");

            // PostgreSQL 主键自增列优先输出原生 IDENTITY，而不是 legacy nextval 默认值。
            if (_currentEnv?.DbType == DataBaseType.PostgreSQL && columnAttr?.IsPrimaryKey == true && columnAttr.IsIdentity)
            {
                sb.Append(" GENERATED BY DEFAULT AS IDENTITY");
            }

            if (!isNullable)
            {
                sb.Append(" NOT NULL");
            }

            var defaultClause = BuildDefaultClause(propType, isNullable, columnAttr);
            if (!string.IsNullOrEmpty(defaultClause))
            {
                sb.Append($" DEFAULT {defaultClause}");
            }

            // 添加注释（优先使用 ColumnDescription，其次使用 Summary）
            var description = GetColumnDescription(prop, columnAttr);
            if (_currentEnv?.DbType != DataBaseType.PostgreSQL && !string.IsNullOrEmpty(description))
            {
                sb.Append($" COMMENT '{description.Replace("'", "\\'")}'");
            }

            return sb.ToString();
        }

        private List<string> GeneratePostgresAddColumnMigrationSql(string tableName, PropertyInfo prop, SugarColumn? columnAttr)
        {
            var sqlList = new List<string>();
            var columnDef = GenerateColumnDefinition(prop, columnAttr);
            var columnName = columnAttr?.ColumnName ?? prop.Name;
            var description = GetColumnDescription(prop, columnAttr);

            // PostgreSQL 列注释走 COMMENT ON COLUMN，不能内联在 ADD COLUMN 语句中。
            sqlList.Add($"ALTER TABLE {QuoteIdentifier(tableName)} ADD COLUMN {columnDef};");

            if (!string.IsNullOrEmpty(description) && _dialect != null)
            {
                sqlList.Add($"{_dialect.BuildSetColumnCommentSql(tableName, columnName, description)};");
            }

            return sqlList;
        }

        private List<string> GeneratePostgresAlterColumnMigrationSql(string tableName, PropertyInfo prop, SugarColumn? columnAttr, DbColumnInfo dbColumn)
        {
            var sqlList = new List<string>();
            var columnName = columnAttr?.ColumnName ?? prop.Name;
            var quotedTable = QuoteIdentifier(tableName);
            var quotedColumn = QuoteIdentifier(columnName);
            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var entityNullable = columnAttr?.IsNullable ?? Nullable.GetUnderlyingType(prop.PropertyType) != null;
            var targetDataType = ResolveColumnDataType(propType, columnAttr);
            var hasTypeChange = HasColumnTypeChange(prop, columnAttr, dbColumn);
            var hasCrossFamilyTypeChange = !IsEquivalentDatabaseType(propType, dbColumn);

            // PostgreSQL 没有 MODIFY COLUMN，需要按变更维度分别输出。
            if (hasTypeChange)
            {
                sqlList.Add(BuildPostgresAlterTypeSql(quotedTable, quotedColumn, targetDataType, hasCrossFamilyTypeChange));
            }

            if (entityNullable != dbColumn.IsNullable)
            {
                sqlList.Add(entityNullable
                    ? $"ALTER TABLE {quotedTable} ALTER COLUMN {quotedColumn} DROP NOT NULL;"
                    : $"ALTER TABLE {quotedTable} ALTER COLUMN {quotedColumn} SET NOT NULL;");
            }

            var description = GetColumnDescription(prop, columnAttr).Trim();
            var dbDescription = (dbColumn.ColumnDescription ?? "").Trim();
            if (!string.IsNullOrEmpty(description) && !string.Equals(description, dbDescription, StringComparison.Ordinal) && _dialect != null)
            {
                sqlList.Add($"{_dialect.BuildSetColumnCommentSql(tableName, columnName, description, dbColumn.DataType)};");
            }

            return sqlList;
        }

        private bool HasColumnTypeChange(PropertyInfo prop, SugarColumn? columnAttr, DbColumnInfo dbColumn)
        {
            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            if (!string.IsNullOrEmpty(columnAttr?.ColumnDataType))
            {
                return !string.Equals(columnAttr.ColumnDataType, dbColumn.DataType, StringComparison.OrdinalIgnoreCase);
            }

            if (propType == typeof(string))
            {
                var entityLength = columnAttr?.Length ?? 0;
                if (entityLength > 0)
                {
                    return dbColumn.Length != entityLength;
                }

                return !dbColumn.DataType.Equals("text", StringComparison.OrdinalIgnoreCase);
            }

            if (propType == typeof(decimal) && columnAttr != null && columnAttr.DecimalDigits > 0)
            {
                return dbColumn.Scale != columnAttr.DecimalDigits;
            }

            return false;
        }

        private string ResolveColumnDataType(Type propType, SugarColumn? columnAttr)
        {
            if (!string.IsNullOrEmpty(columnAttr?.ColumnDataType))
            {
                return columnAttr.ColumnDataType;
            }

            if (IsJsonNetType(propType) || columnAttr?.IsJson == true)
            {
                return _currentEnv?.DbType == DataBaseType.PostgreSQL ? "jsonb" : "json";
            }

            return _currentEnv?.DbType == DataBaseType.PostgreSQL
                ? GetPostgreSqlDataType(propType, columnAttr)
                : GetMySqlDataType(propType, columnAttr);
        }

        private string BuildDefaultClause(Type propType, bool isNullable, SugarColumn? columnAttr)
        {
            // identity 列默认值由数据库维护，迁移 SQL 不再显式拼 DEFAULT。
            if (columnAttr?.IsPrimaryKey == true && columnAttr.IsIdentity && _currentEnv?.DbType == DataBaseType.PostgreSQL)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(columnAttr?.DefaultValue))
            {
                return FormatDefaultValue(propType, columnAttr.DefaultValue);
            }

            if (isNullable)
            {
                return string.Empty;
            }

            if (propType == typeof(int) || propType == typeof(long) || propType == typeof(byte) || propType == typeof(short))
            {
                return "0";
            }

            if (propType == typeof(decimal) || propType == typeof(float) || propType == typeof(double))
            {
                return "0";
            }

            if (propType == typeof(string))
            {
                return "''";
            }

            if (propType == typeof(bool))
            {
                return _currentEnv?.DbType == DataBaseType.PostgreSQL ? "false" : "0";
            }

            if (IsJsonNetType(propType) || columnAttr?.IsJson == true)
            {
                return string.Empty;
            }

            return string.Empty;
        }

        private string FormatDefaultValue(Type propType, string defaultValue)
        {
            if (propType == typeof(int) || propType == typeof(long) || propType == typeof(decimal) ||
                propType == typeof(float) || propType == typeof(double) || propType == typeof(byte) ||
                propType == typeof(short))
            {
                return defaultValue;
            }

            if (propType == typeof(bool))
            {
                if (_currentEnv?.DbType == DataBaseType.PostgreSQL)
                {
                    // PostgreSQL 布尔默认值统一输出 true/false，避免沿用 1/0 风格。
                    return defaultValue == "1" ? "true" : defaultValue == "0" ? "false" : defaultValue.ToLowerInvariant();
                }

                return defaultValue;
            }

            return $"'{defaultValue.Replace("'", "''")}'";
        }

        private string GetMySqlDataType(Type propType, SugarColumn? columnAttr)
        {
            var length = columnAttr?.Length ?? 0;
            var decimalDigits = columnAttr?.DecimalDigits ?? 2;

            if (propType == typeof(int))
                return "int";
            if (propType == typeof(long))
                return "bigint";
            if (propType == typeof(short))
                return "smallint";
            if (propType == typeof(byte))
                return "tinyint";
            if (propType == typeof(bool))
                return "tinyint(1)";
            if (propType == typeof(decimal))
                return $"decimal({(length > 0 ? length : 18)},{decimalDigits})";
            if (propType == typeof(float))
                return "float";
            if (propType == typeof(double))
                return "double";
            if (propType == typeof(DateTime))
                return "datetime";
            if (propType == typeof(string))
            {
                if (length > 0 && length <= 8000)
                    return $"varchar({length})";
                return "text";
            }
            if (propType == typeof(Guid))
                return "char(36)";
            if (propType == typeof(byte[]))
                return "blob";
            if (IsJsonNetType(propType))
                return "json";

            return "varchar(255)";
        }

        private string GetPostgreSqlDataType(Type propType, SugarColumn? columnAttr)
        {
            var length = columnAttr?.Length ?? 0;
            var decimalDigits = columnAttr?.DecimalDigits ?? 2;

            if (propType == typeof(int))
                return "integer";
            if (propType == typeof(long))
                return "bigint";
            if (propType == typeof(short))
                return "smallint";
            if (propType == typeof(byte))
                return "smallint";
            if (propType == typeof(bool))
                return "boolean";
            if (propType == typeof(decimal))
                return $"numeric({(length > 0 ? length : 18)},{decimalDigits})";
            if (propType == typeof(float))
                return "real";
            if (propType == typeof(double))
                return "double precision";
            if (propType == typeof(DateTime))
                return "timestamp";
            if (propType == typeof(string))
            {
                if (length > 0 && length <= 10485760)
                    return $"varchar({length})";
                return "text";
            }
            if (propType == typeof(Guid))
                return "uuid";
            if (propType == typeof(byte[]))
                return "bytea";
            if (IsJsonNetType(propType))
                return "jsonb";

            return "varchar(255)";
        }

        private string QuoteIdentifier(string identifier)
        {
            return _dialect?.QuoteIdentifier(identifier) ?? $"`{identifier}`";
        }

        private void SaveMigrationSql(List<string> sqlLines)
        {
            try
            {
                var logDir = _configuration.LogPath;
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var fileName = $"migration_{DateTime.Now:yyyy-MM-dd_HHmmss}.sql";
                var filePath = Path.Combine(logDir, fileName);
                File.WriteAllLines(filePath, sqlLines);
                PrintSuccess($"SQL 脚本已保存到: {filePath}");
            }
            catch (Exception ex)
            {
                PrintError($"保存失败: {ex.Message}");
            }
        }

        private void ExecuteMigrationSql(List<string> sqlLines)
        {
            var executableSql = sqlLines
                .Where(s => !string.IsNullOrWhiteSpace(s) && !s.TrimStart().StartsWith("--"))
                .ToList();

            if (executableSql.Count == 0)
            {
                PrintInfo("没有可执行的 SQL");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("正在执行迁移 SQL...");
            Console.WriteLine();

            int success = 0, failed = 0;

            foreach (var sql in executableSql)
            {
                try
                {
                    _db!.Ado.ExecuteCommand(sql);
                    PrintSuccess($"  ✓ {sql.Substring(0, Math.Min(60, sql.Length))}...");
                    success++;
                }
                catch (Exception ex)
                {
                    PrintError($"  ✗ {sql.Substring(0, Math.Min(40, sql.Length))}... 失败: {ex.Message}");
                    failed++;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"执行完成！成功: {success}, 失败: {failed}");
        }

        #endregion

        #region 查看差异

        private void ViewDifferences()
        {
            Console.WriteLine("请选择要查看差异的表类型：");
            Console.WriteLine("  [1] 普通表");
            Console.WriteLine("  [2] 分表");
            Console.WriteLine("  [b] 返回");
            Console.WriteLine();
            Console.Write("请输入选择: ");

            var input = Console.ReadLine()?.Trim().ToLower();

            List<Type> entities;
            switch (input)
            {
                case "1":
                    entities = _normalEntities;
                    break;
                case "2":
                    entities = _splitEntities;
                    break;
                default:
                    return;
            }

            Console.WriteLine();
            Console.WriteLine("正在分析表结构差异...");
            Console.WriteLine();

            foreach (var entity in entities)
            {
                var tableName = GetTableName(entity);
                var differences = GetTableDifferences(entity);

                if (differences.Count == 0)
                {
                    Console.WriteLine($"[{tableName}] 无差异");
                }
                else
                {
                    Console.WriteLine($"[{tableName}] 检测到 {differences.Count} 处变更：");
                    foreach (var diff in differences)
                    {
                        if (diff.StartsWith("+"))
                            PrintSuccess($"    {diff}");
                        else if (diff.StartsWith("-"))
                            PrintError($"    {diff}");
                        else
                            PrintWarning($"    {diff}");
                    }
                }
                Console.WriteLine();
            }
        }

        #endregion

        #region 辅助方法

        private string GetTableName(Type entityType)
        {
            var attr = entityType.GetCustomAttribute<SugarTable>();
            if (attr != null && !string.IsNullOrEmpty(attr.TableName))
            {
                // 分表的表名模板，去掉占位符部分
                var name = attr.TableName;
                if (name.Contains("{"))
                {
                    name = name.Substring(0, name.IndexOf("{")).TrimEnd('_');
                }
                return name;
            }
            return entityType.Name;
        }

        private string GetTableDescription(Type entityType)
        {
            // 优先使用 SugarTable 的 TableDescription
            var tableAttr = entityType.GetCustomAttribute<SugarTable>();
            if (!string.IsNullOrEmpty(tableAttr?.TableDescription))
            {
                return tableAttr.TableDescription;
            }

            // 尝试从 XML 注释获取 Summary
            var summary = GetClassSummary(entityType);
            return summary ?? "";
        }

        /// <summary>
        /// 获取字段描述：优先使用 ColumnDescription，其次使用 Summary
        /// </summary>
        private string GetColumnDescription(PropertyInfo prop, SugarColumn? columnAttr)
        {
            // 优先使用 ColumnDescription
            if (!string.IsNullOrEmpty(columnAttr?.ColumnDescription))
            {
                return columnAttr.ColumnDescription;
            }

            // 尝试从 XML Summary 获取
            var summary = GetPropertySummary(prop);
            return summary ?? "";
        }

        private int GetSplitTableCount(Type entityType)
        {
            try
            {
                var tables = GetSplitTableNames(entityType);
                return tables.Count;
            }
            catch
            {
                return 0;
            }
        }

        private List<string> GetSplitTableNames(Type entityType)
        {
            try
            {
                // 使用反射调用 SplitHelper<T>().GetTables()
                var method = typeof(SqlSugarClient).GetMethod("SplitHelper", Type.EmptyTypes);
                var genericMethod = method?.MakeGenericMethod(entityType);
                var helper = genericMethod?.Invoke(_db, null);

                if (helper != null)
                {
                    var getTablesMethod = helper.GetType().GetMethod("GetTables");
                    var tables = getTablesMethod?.Invoke(helper, null) as IEnumerable<SplitTableInfo>;
                    return tables?.Select(t => t.TableName).ToList() ?? new List<string>();
                }
            }
            catch
            {
                // 备用方案：直接查询数据库
                var baseTableName = GetTableName(entityType);
                var allTables = _db!.DbMaintenance.GetTableInfoList();
                return allTables
                    .Where(t => t.Name.StartsWith(baseTableName + "_"))
                    .Select(t => t.Name)
                    .ToList();
            }

            return new List<string>();
        }

        private List<string> GetTableDifferences(Type entityType)
        {
            var differences = new List<string>();

            try
            {
                var tableName = GetTableName(entityType);

                // 检查表是否存在
                if (!_db!.DbMaintenance.IsAnyTable(tableName, false))
                {
                    differences.Add($"+ 新建表: {tableName}");
                    return differences;
                }

                // 检查表注释
                var tableInfo = _db.DbMaintenance.GetTableInfoList(false)
                    .FirstOrDefault(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                var tableAttr = entityType.GetCustomAttribute<SugarTable>();
                var entityTableDescription = tableAttr?.TableDescription ?? "";
                var dbTableDescription = tableInfo?.Description ?? "";

                if (!string.Equals(entityTableDescription, dbTableDescription, StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(entityTableDescription))
                {
                    differences.Add($"~ 表注释变更: \"{dbTableDescription}\" → \"{entityTableDescription}\"");
                }

                // 获取数据库中的列
                var dbColumns = _db.DbMaintenance.GetColumnInfosByTableName(tableName, false);
                var dbColumnDict = dbColumns.ToDictionary(c => c.DbColumnName.ToLower(), c => c);
                var dbColumnNames = dbColumns.Select(c => c.DbColumnName.ToLower()).ToHashSet();

                // 获取实体中的属性
                var properties = entityType.GetProperties()
                    .Where(p => p.GetCustomAttribute<SugarColumn>()?.IsIgnore != true)
                    .ToList();

                foreach (var prop in properties)
                {
                    var columnAttr = prop.GetCustomAttribute<SugarColumn>();
                    var columnName = columnAttr?.ColumnName ?? prop.Name;
                    var columnNameLower = columnName.ToLower();

                    if (!dbColumnNames.Contains(columnNameLower))
                    {
                        differences.Add($"+ 新增字段: {columnName}");
                    }
                    else
                    {
                        var dbColumn = dbColumnDict[columnNameLower];

                        // 检查字段类型/长度/可空性变更
                        var columnDiffs = CompareColumnAttributes(prop, columnAttr, dbColumn);
                        differences.AddRange(columnDiffs);
                    }
                }

                // 检查是否有字段被删除
                var entityColumnNames = properties
                    .Select(p => (p.GetCustomAttribute<SugarColumn>()?.ColumnName ?? p.Name).ToLower())
                    .ToHashSet();

                foreach (var dbColumn in dbColumns)
                {
                    if (!entityColumnNames.Contains(dbColumn.DbColumnName.ToLower()))
                    {
                        differences.Add($"- 删除字段: {dbColumn.DbColumnName}");
                    }
                }

                // 检查索引差异
                var indexDifferences = GetIndexDifferences(entityType, tableName);
                differences.AddRange(indexDifferences);
            }
            catch (Exception ex)
            {
                differences.Add($"! 分析失败: {ex.Message}");
            }

            return differences;
        }

        private List<string> GetIndexDifferences(Type entityType, string tableName)
        {
            var differences = new List<string>();

            try
            {
                // 获取实体中定义的索引（通过 Attribute 数据）
                var entityIndexNames = new List<string>();

                // 从属性数据中获取 SugarIndex 特性
                var attrs = entityType.GetCustomAttributesData();

                foreach (var attr in attrs)
                {
                    if (attr.AttributeType.Name == "SugarIndexAttribute")
                    {
                        // 构造函数参数：第0个是索引名
                        if (attr.ConstructorArguments.Count > 0)
                        {
                            var indexName = attr.ConstructorArguments[0].Value?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(indexName))
                            {
                                entityIndexNames.Add(indexName);
                            }
                        }
                    }
                }

                // 获取数据库中的索引列表
                var dbIndexNames = _db!.DbMaintenance.GetIndexList(tableName)
                    .Where(name => !name.ToLower().Contains("primary")) // 排除主键索引
                    .ToList();

                // 规范化数据库索引名（去掉分表名后缀），用于对比
                // 同时去掉末尾的下划线，以便与实体中定义的索引名匹配
                var normalizedDbIndexes = dbIndexNames
                    .Select(name => NormalizeIndexName(name, tableName).TrimEnd('_').ToLower())
                    .ToHashSet();

                // 检查新增的索引
                foreach (var indexName in entityIndexNames)
                {
                    var normalizedEntityIndex = indexName.TrimEnd('_').ToLower();
                    if (!string.IsNullOrEmpty(indexName) && !normalizedDbIndexes.Contains(normalizedEntityIndex))
                    {
                        differences.Add($"+ 新增索引: {indexName}");
                    }
                }
            }
            catch (Exception ex)
            {
                differences.Add($"! 索引分析失败: {ex.Message}");
            }

            return differences;
        }

        /// <summary>
        /// 规范化数据库索引名，去掉分表名后缀
        /// 只有分表索引（带日期后缀）才需要规范化，普通表索引直接返回原名
        /// 例如：idx_status_ssp_placement_20260201 -> idx_status（分表索引）
        /// 例如：uk_ssp_placement_key -> uk_ssp_placement_key（普通表索引，不变）
        /// </summary>
        private string NormalizeIndexName(string dbIndexName, string tableName)
        {
            // 检查是否有日期后缀（6-8位数字）
            var datePattern = @"_\d{6,8}$";
            if (!Regex.IsMatch(dbIndexName, datePattern))
            {
                // 没有日期后缀，是普通表索引，直接返回原名
                return dbIndexName;
            }

            // 有日期后缀，是分表索引，需要去掉表名和日期后缀
            // 例如：idx_status_ssp_placement_20260201 -> idx_status

            // 先去掉日期后缀
            var withoutDate = Regex.Replace(dbIndexName, datePattern, "");

            // 再去掉表名后缀
            if (withoutDate.EndsWith(tableName, StringComparison.OrdinalIgnoreCase))
            {
                return withoutDate.Substring(0, withoutDate.Length - tableName.Length).TrimEnd('_');
            }

            // 处理表名在中间的情况
            var tableIndex = withoutDate.IndexOf(tableName, StringComparison.OrdinalIgnoreCase);
            if (tableIndex > 0)
            {
                return withoutDate.Substring(0, tableIndex).TrimEnd('_');
            }

            return withoutDate;
        }

        private sealed class IndexDefinition
        {
            public string Name { get; init; } = string.Empty;

            public List<string> Fields { get; init; } = new();

            public bool IsUnique { get; init; }
        }

        private sealed class PostgresIdentityColumnInfo
        {
            public string? IsIdentity { get; init; }

            public string? IdentityGeneration { get; init; }

            public string? ColumnDefault { get; init; }

            public string? SequenceName { get; init; }
        }

        private List<IndexDefinition> GetEntityIndexes(Type entityType)
        {
            var definitions = new List<IndexDefinition>();
            var attrs = entityType.GetCustomAttributesData();

            foreach (var attr in attrs)
            {
                if (attr.AttributeType.Name != "SugarIndexAttribute" || attr.ConstructorArguments.Count == 0)
                {
                    continue;
                }

                var indexName = attr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(indexName))
                {
                    continue;
                }

                var fields = new List<string>();
                bool isUnique = false;

                for (int i = 1; i < attr.ConstructorArguments.Count; i++)
                {
                    var argument = attr.ConstructorArguments[i];
                    if (argument.ArgumentType == typeof(string) && argument.Value is string fieldName && !string.IsNullOrWhiteSpace(fieldName))
                    {
                        fields.Add(fieldName);
                        continue;
                    }

                    if (argument.ArgumentType == typeof(bool) && argument.Value is bool ctorUnique)
                    {
                        isUnique = ctorUnique;
                    }
                }

                foreach (var namedArg in attr.NamedArguments)
                {
                    if (namedArg.MemberName.Equals("isUnique", StringComparison.OrdinalIgnoreCase) &&
                        namedArg.TypedValue.ArgumentType == typeof(bool) &&
                        namedArg.TypedValue.Value is bool namedUnique)
                    {
                        isUnique = namedUnique;
                    }
                }

                if (fields.Count == 0)
                {
                    continue;
                }

                definitions.Add(new IndexDefinition
                {
                    Name = indexName,
                    Fields = fields,
                    IsUnique = isUnique
                });
            }

            return definitions;
        }

        private void CreateIndex(string tableName, Type entityType, string indexName)
        {
            try
            {
                if (_db == null || _dialect == null)
                {
                    return;
                }

                foreach (var definition in GetEntityIndexes(entityType))
                {
                    if (!string.Equals(definition.Name, indexName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var sql = _dialect.BuildCreateIndexSql(tableName, definition.Name, definition.Fields, definition.IsUnique);
                    _db.Ado.ExecuteCommand(sql);
                    Console.WriteLine($"    创建索引: {definition.Name} on ({string.Join(", ", definition.Fields)})");
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    创建索引失败: {indexName}, 错误: {ex.Message}");
            }
        }

        private void CreateIndexesFromEntity(string tableName, Type entityType)
        {
            try
            {
                if (_db == null || _dialect == null)
                {
                    return;
                }

                // 获取数据库中已有的索引
                var dbIndexes = _db.DbMaintenance.GetIndexList(tableName)
                    .Select(i => i.ToLower())
                    .ToHashSet();

                foreach (var definition in GetEntityIndexes(entityType))
                {
                    if (dbIndexes.Contains(definition.Name.ToLower()))
                    {
                        Console.WriteLine($"    索引已存在: {definition.Name}");
                        continue;
                    }

                    var sql = _dialect.BuildCreateIndexSql(tableName, definition.Name, definition.Fields, definition.IsUnique);
                    _db.Ado.ExecuteCommand(sql);
                    Console.WriteLine($"    创建索引: {definition.Name} on ({string.Join(", ", definition.Fields)})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    创建索引失败: {ex.Message}");
            }
        }

        /// <summary>
        /// PostgreSQL 下对齐主键自增列：
        /// 1. 优先将 legacy sequence/default 方案收敛为原生 identity 列
        /// 2. 修复 sequence 当前值，避免插入时主键冲突
        /// </summary>
        private void RepairPostgresIdentityDefaults(string tableName, Type entityType)
        {
            try
            {
                if (_db == null || _currentEnv == null || _dialect == null)
                {
                    return;
                }

                if (_currentEnv.DbType != DataBaseType.PostgreSQL || !_currentEnv.RepairPostgresIdentity)
                {
                    return;
                }

                var properties = entityType.GetProperties()
                    .Where(p => p.GetCustomAttribute<SugarColumn>()?.IsIgnore != true)
                    .ToList();

                int repaired = 0;
                foreach (var prop in properties)
                {
                    var columnAttr = prop.GetCustomAttribute<SugarColumn>();
                    if (columnAttr?.IsPrimaryKey != true || columnAttr.IsIdentity != true)
                    {
                        continue;
                    }

                    var columnName = columnAttr.ColumnName ?? prop.Name;
                    var quotedTableName = _dialect.QuoteIdentifier(tableName);
                    var quotedColumnName = _dialect.QuoteIdentifier(columnName);
                    var identityInfo = GetPostgresIdentityColumnInfo(tableName, columnName);
                    if (identityInfo == null)
                    {
                        continue;
                    }

                    // 老表如果还是 sequence + default 方案，这里主动收敛为原生 identity。
                    if (!string.Equals(identityInfo.IsIdentity, "YES", StringComparison.OrdinalIgnoreCase))
                    {
                        ConvertPostgresColumnToIdentity(tableName, columnName, quotedTableName, quotedColumnName, identityInfo);
                        identityInfo = GetPostgresIdentityColumnInfo(tableName, columnName);
                    }
                    else if (string.Equals(identityInfo.IdentityGeneration, "ALWAYS", StringComparison.OrdinalIgnoreCase))
                    {
                        _db.Ado.ExecuteCommand(
                            $"ALTER TABLE {quotedTableName} ALTER COLUMN {quotedColumnName} SET GENERATED BY DEFAULT");
                        identityInfo = GetPostgresIdentityColumnInfo(tableName, columnName);
                    }

                    if (identityInfo != null && !string.IsNullOrWhiteSpace(identityInfo.SequenceName))
                    {
                        AlignPostgresSequenceValue(quotedTableName, quotedColumnName, identityInfo.SequenceName);
                    }

                    repaired++;
                }

                if (repaired > 0)
                {
                    Console.WriteLine($"    对齐 PostgreSQL 自增列: {repaired} 个");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    对齐 PostgreSQL 自增列失败: {ex.Message}");
            }
        }

        private PostgresIdentityColumnInfo? GetPostgresIdentityColumnInfo(string tableName, string columnName)
        {
            if (_db == null)
            {
                return null;
            }

            const string sql = @"
SELECT
    c.is_identity AS IsIdentity,
    c.identity_generation AS IdentityGeneration,
    c.column_default AS ColumnDefault,
    pg_get_serial_sequence(format('%I.%I', current_schema(), @tableName), @columnName) AS SequenceName
FROM information_schema.columns c
WHERE c.table_schema = current_schema()
  AND c.table_name = @tableName
  AND c.column_name = @columnName
LIMIT 1;";

            return _db.Ado.SqlQuerySingle<PostgresIdentityColumnInfo>(sql, new { tableName, columnName });
        }

        private void ConvertPostgresColumnToIdentity(
            string tableName,
            string columnName,
            string quotedTableName,
            string quotedColumnName,
            PostgresIdentityColumnInfo identityInfo)
        {
            if (_db == null || _dialect == null)
            {
                return;
            }

            string? legacySequenceName = identityInfo.SequenceName;
            string? legacySequenceToDrop = null;

            // 先移除旧默认值，否则 PostgreSQL 不允许直接追加 identity 属性。
            _db.Ado.ExecuteCommand($"ALTER TABLE {quotedTableName} ALTER COLUMN {quotedColumnName} DROP DEFAULT");

            if (!string.IsNullOrWhiteSpace(legacySequenceName))
            {
                var legacySequenceParts = SplitQualifiedIdentifier(legacySequenceName);
                var legacySequenceBaseName = legacySequenceParts[^1];
                var legacySequenceRenamed = $"{legacySequenceBaseName}_legacy_cf_{DateTime.UtcNow:yyyyMMddHHmmssfff}";

                // 先临时改名，避免 PostgreSQL 生成的新 identity sequence 与旧 sequence 重名冲突。
                _db.Ado.ExecuteCommand(
                    $"ALTER SEQUENCE {QuoteQualifiedIdentifier(legacySequenceName)} RENAME TO {_dialect.QuoteIdentifier(legacySequenceRenamed)}");

                legacySequenceToDrop = legacySequenceParts.Length > 1
                    ? string.Join(".", legacySequenceParts.Take(legacySequenceParts.Length - 1).Append(legacySequenceRenamed))
                    : legacySequenceRenamed;
            }

            _db.Ado.ExecuteCommand(
                $"ALTER TABLE {quotedTableName} ALTER COLUMN {quotedColumnName} ADD GENERATED BY DEFAULT AS IDENTITY");

            var newIdentityInfo = GetPostgresIdentityColumnInfo(tableName, columnName);
            if (newIdentityInfo == null || string.IsNullOrWhiteSpace(newIdentityInfo.SequenceName))
            {
                throw new InvalidOperationException($"列 {tableName}.{columnName} 转换为 identity 后未找到关联 sequence。");
            }

            AlignPostgresSequenceValue(quotedTableName, quotedColumnName, newIdentityInfo.SequenceName);

            if (!string.IsNullOrWhiteSpace(legacySequenceToDrop) &&
                !string.Equals(NormalizeQualifiedIdentifier(legacySequenceToDrop), NormalizeQualifiedIdentifier(newIdentityInfo.SequenceName), StringComparison.OrdinalIgnoreCase))
            {
                _db.Ado.ExecuteCommand($"DROP SEQUENCE IF EXISTS {QuoteQualifiedIdentifier(legacySequenceToDrop)}");
            }
        }

        private void AlignPostgresSequenceValue(string quotedTableName, string quotedColumnName, string sequenceName)
        {
            if (_db == null)
            {
                return;
            }

            // 将 sequence 调整到当前最大主键之后，避免迁移后插入出现 duplicate key。
            var nextValue = _db.Ado.SqlQuerySingle<long>(
                $"SELECT COALESCE(MAX({quotedColumnName}), 0) + 1 FROM {quotedTableName}");

            _db.Ado.SqlQuerySingle<long>(
                "SELECT setval(@sequenceName, @nextValue, false);",
                new
                {
                    sequenceName,
                    nextValue
                });
        }

        private string QuoteQualifiedIdentifier(string identifier)
        {
            var parts = SplitQualifiedIdentifier(identifier);
            return string.Join(".", parts.Select(p => _dialect!.QuoteIdentifier(p)));
        }

        private static string[] SplitQualifiedIdentifier(string identifier)
        {
            return identifier
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().Trim('"'))
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
        }

        private static string NormalizeQualifiedIdentifier(string identifier)
        {
            return string.Join(".", SplitQualifiedIdentifier(identifier)).ToLowerInvariant();
        }

        private bool IsCommentOnlyDifferences(List<string> differences)
        {
            return differences.Count > 0
                && differences.All(d =>
                    d.StartsWith("~ 表注释变更:", StringComparison.Ordinal) ||
                    d.StartsWith("~ 字段注释变更 [", StringComparison.Ordinal));
        }

        /// <summary>
        /// 同步表注释到数据库（优先使用 SugarTable.TableDescription，其次使用 Summary）
        /// </summary>
        private void SyncTableComment(string tableName, Type entityType)
        {
            try
            {
                if (_db == null || _dialect == null)
                {
                    return;
                }

                var entityDescription = GetTableDescription(entityType);
                if (string.IsNullOrEmpty(entityDescription))
                {
                    return;
                }

                var tableInfo = _db.DbMaintenance.GetTableInfoList(false)
                    .FirstOrDefault(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));
                var dbDescription = tableInfo?.Description ?? string.Empty;

                if (string.Equals(entityDescription, dbDescription, StringComparison.Ordinal))
                {
                    return;
                }

                var sql = _dialect.BuildSetTableCommentSql(tableName, entityDescription);
                _db.Ado.ExecuteCommand(sql);
                Console.WriteLine("    同步表注释: 1 个");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    同步表注释失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 同步字段注释到数据库（从 ColumnDescription 或 Summary）
        /// </summary>
        private void SyncColumnComments(string tableName, Type entityType)
        {
            try
            {
                if (_db == null || _dialect == null)
                {
                    return;
                }

                // 获取数据库中的列信息
                var dbColumns = _db.DbMaintenance.GetColumnInfosByTableName(tableName, false);
                var dbColumnDict = dbColumns.ToDictionary(c => c.DbColumnName.ToLower(), c => c);

                // 获取实体中的属性
                var properties = entityType.GetProperties()
                    .Where(p => p.GetCustomAttribute<SugarColumn>()?.IsIgnore != true)
                    .ToList();

                int updated = 0;
                foreach (var prop in properties)
                {
                    var columnAttr = prop.GetCustomAttribute<SugarColumn>();
                    var columnName = columnAttr?.ColumnName ?? prop.Name;
                    var columnNameLower = columnName.ToLower();

                    if (!dbColumnDict.ContainsKey(columnNameLower))
                    {
                        continue; // 新字段会在 InitTables 时创建
                    }

                    // 获取字段描述（优先使用 ColumnDescription，其次使用 Summary）
                    var entityDescription = GetColumnDescription(prop, columnAttr);
                    if (string.IsNullOrEmpty(entityDescription))
                    {
                        continue; // 没有描述则跳过
                    }

                    var dbColumn = dbColumnDict[columnNameLower];
                    var dbDescription = dbColumn.ColumnDescription ?? "";

                    // 只有描述发生变化时才更新
                    if (!string.Equals(entityDescription, dbDescription, StringComparison.Ordinal))
                    {
                        var sql = _dialect.BuildSetColumnCommentSql(tableName, columnName, entityDescription, dbColumn.DataType);
                        _db.Ado.ExecuteCommand(sql);
                        updated++;
                    }
                }

                if (updated > 0)
                {
                    Console.WriteLine($"    同步字段注释: {updated} 个");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    同步字段注释失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 比较实体字段属性与数据库字段的差异（类型、长度、可空性、注释）
        /// </summary>
        private List<string> CompareColumnAttributes(PropertyInfo prop, SugarColumn? columnAttr, DbColumnInfo dbColumn)
        {
            var diffs = new List<string>();
            var columnName = columnAttr?.ColumnName ?? prop.Name;
            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var hasExplicitColumnType = !string.IsNullOrEmpty(columnAttr?.ColumnDataType);
            var isJsonColumn = columnAttr?.IsJson == true || IsJsonNetType(propType);

            // 只有在没有显式 ColumnDataType 且不是 JSON 列时，才用 CLR 类型族比较，避免 jsonb → jsonb 被误报。
            if (!hasExplicitColumnType && !isJsonColumn)
            {
                var targetDataType = ResolveColumnDataType(propType, columnAttr);
                if (!IsEquivalentDatabaseType(propType, dbColumn))
                {
                    diffs.Add($"~ 字段类型变更 [{columnName}]: {dbColumn.DataType} → {targetDataType}");
                }
            }

            // 检查字段长度变更
            var entityLength = columnAttr?.Length ?? 0;
            if (entityLength > 0 && propType == typeof(string))
            {
                var dbLength = dbColumn.Length;
                if (entityLength != dbLength)
                {
                    diffs.Add($"~ 字段长度变更 [{columnName}]: {dbLength} → {entityLength}");
                }
            }

            // 检查可空性变更
            var entityNullable = columnAttr?.IsNullable ?? Nullable.GetUnderlyingType(prop.PropertyType) != null;
            if (entityNullable != dbColumn.IsNullable)
            {
                var from = dbColumn.IsNullable ? "可空" : "非空";
                var to = entityNullable ? "可空" : "非空";
                diffs.Add($"~ 字段可空性变更 [{columnName}]: {from} → {to}");
            }

            // 检查字段类型变更（仅在指定了 ColumnDataType 时比较）
            if (!string.IsNullOrEmpty(columnAttr?.ColumnDataType))
            {
                var entityDataType = columnAttr!.ColumnDataType.ToLower();
                var dbDataType = dbColumn.DataType?.ToLower() ?? "";
                if (!string.Equals(entityDataType, dbDataType, StringComparison.OrdinalIgnoreCase))
                {
                    diffs.Add($"~ 字段类型变更 [{columnName}]: {dbColumn.DataType} → {columnAttr.ColumnDataType}");
                }
            }

            // 检查小数精度变更（decimal 类型）
            if (propType == typeof(decimal) && columnAttr != null)
            {
                var entityDecimalDigits = columnAttr.DecimalDigits;
                if (entityDecimalDigits > 0 && dbColumn.Scale != entityDecimalDigits)
                {
                    diffs.Add($"~ 字段精度变更 [{columnName}]: Scale {dbColumn.Scale} → {entityDecimalDigits}");
                }
            }

            // 检查字段注释变更
            var entityDescription = GetColumnDescription(prop, columnAttr).Trim();
            var dbDescription = (dbColumn.ColumnDescription ?? "").Trim();
            if (!string.Equals(entityDescription, dbDescription, StringComparison.Ordinal)
                && !string.IsNullOrEmpty(entityDescription))
            {
                diffs.Add($"~ 字段注释变更 [{columnName}]: \"{TruncateString(dbDescription, 20)}\" → \"{TruncateString(entityDescription, 20)}\"");
            }

            return diffs;
        }

        private string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        private List<string> GetBlockingPostgresTypeChanges(Type entityType, string tableName)
        {
            var blockingChanges = new List<string>();

            if (_db == null || _currentEnv?.DbType != DataBaseType.PostgreSQL)
            {
                return blockingChanges;
            }

            if (IsTableEmpty(tableName))
            {
                return blockingChanges;
            }

            var dbColumns = _db.DbMaintenance.GetColumnInfosByTableName(tableName, false)
                .ToDictionary(c => c.DbColumnName.ToLower(), c => c);

            var properties = entityType.GetProperties()
                .Where(p => p.GetCustomAttribute<SugarColumn>()?.IsIgnore != true)
                .ToList();

            foreach (var prop in properties)
            {
                var columnAttr = prop.GetCustomAttribute<SugarColumn>();
                var columnName = columnAttr?.ColumnName ?? prop.Name;
                if (!dbColumns.TryGetValue(columnName.ToLower(), out var dbColumn))
                {
                    continue;
                }

                var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                if (IsEquivalentDatabaseType(propType, dbColumn))
                {
                    continue;
                }

                var dbFamily = GetDatabaseTypeFamily(dbColumn.DataType);
                var entityFamily = GetPropertyTypeFamily(propType);
                if (dbFamily != entityFamily)
                {
                    var targetType = ResolveColumnDataType(propType, columnAttr);
                    blockingChanges.Add($"字段 [{columnName}] 类型需从 {dbColumn.DataType} 转为 {targetType}，PostgreSQL 可能需要 USING 显式转换");
                }
            }

            return blockingChanges;
        }

        private void ExecuteCreateTable(Type entity, string tableName)
        {
            if (_db == null)
            {
                throw new InvalidOperationException("数据库连接未初始化。");
            }

            var isSplitTemplate = _splitEntities.Contains(entity);
            if (isSplitTemplate)
            {
                _db.MappingTables.Add(entity.Name, tableName);
                _db.CodeFirst.InitTables(entity);
                _db.MappingTables.RemoveAll(m => m.EntityName == entity.Name);
            }
            else
            {
                _db.CodeFirst.InitTables(entity);
            }

            if (!IsTableExists(tableName))
            {
                throw new InvalidOperationException($"建表后仍未找到表 {tableName}");
            }
        }

        private bool TryApplyPostgresManagedMigration(Type entityType, string tableName)
        {
            if (_db == null || _currentEnv?.DbType != DataBaseType.PostgreSQL || !IsTableExists(tableName))
            {
                return false;
            }

            var sqlLines = GenerateEntityMigrationSql(entityType, false);
            if (sqlLines.Count == 0)
            {
                return false;
            }

            var executed = false;
            foreach (var sql in sqlLines)
            {
                var trimmedSql = sql.Trim();
                if (string.IsNullOrWhiteSpace(trimmedSql))
                {
                    continue;
                }

                if (trimmedSql.StartsWith("-- [危险] ALTER TABLE", StringComparison.OrdinalIgnoreCase) && _currentEnv.AllowDeleteColumn)
                {
                    // 删除列默认保留为危险操作，仅在显式允许时执行。
                    var executableSql = trimmedSql.Replace("-- [危险] ", "", StringComparison.Ordinal);
                    _db.Ado.ExecuteCommand(executableSql.TrimEnd(';'));
                    executed = true;
                    continue;
                }

                if (trimmedSql.StartsWith("--", StringComparison.Ordinal))
                {
                    continue;
                }

                _db.Ado.ExecuteCommand(trimmedSql.TrimEnd(';'));
                executed = true;
            }

            return executed;
        }

        private bool IsTableEmpty(string tableName)
        {
            if (_db == null)
            {
                return false;
            }

            if (!IsTableExists(tableName))
            {
                return false;
            }

            var sql = $"SELECT 1 FROM {QuoteIdentifier(tableName)} LIMIT 1;";
            var hasRows = _db.Ado.SqlQuery<int>(sql);
            return hasRows == null || hasRows.Count == 0;
        }

        private bool IsTableExists(string tableName)
        {
            return _db != null && _db.DbMaintenance.IsAnyTable(tableName, false);
        }

        private string BuildPostgresAlterTypeSql(string quotedTable, string quotedColumn, string targetDataType, bool hasCrossFamilyTypeChange)
        {
            if (hasCrossFamilyTypeChange)
            {
                // 跨类型家族时显式指定 USING，避免 PostgreSQL 因缺少自动 cast 而报 42804。
                return $"ALTER TABLE {quotedTable} ALTER COLUMN {quotedColumn} TYPE {targetDataType} USING {quotedColumn}::{targetDataType};";
            }

            return $"ALTER TABLE {quotedTable} ALTER COLUMN {quotedColumn} TYPE {targetDataType};";
        }

        private bool IsEquivalentDatabaseType(Type propType, DbColumnInfo dbColumn)
        {
            return GetPropertyTypeFamily(propType) == GetDatabaseTypeFamily(dbColumn.DataType);
        }

        private bool IsJsonNetType(Type propType)
        {
            var fullName = propType.FullName ?? string.Empty;
            return fullName == "Newtonsoft.Json.Linq.JObject"
                || fullName == "Newtonsoft.Json.Linq.JArray"
                || fullName == "Newtonsoft.Json.Linq.JToken";
        }

        private string GetPropertyTypeFamily(Type propType)
        {
            if (IsJsonNetType(propType))
                return "json";
            if (propType == typeof(int))
                return "int";
            if (propType == typeof(long))
                return "bigint";
            if (propType == typeof(short) || propType == typeof(byte))
                return "smallint";
            if (propType == typeof(decimal))
                return "decimal";
            if (propType == typeof(float))
                return "float";
            if (propType == typeof(double))
                return "double";
            if (propType == typeof(bool))
                return "bool";
            if (propType == typeof(DateTime))
                return "datetime";
            if (propType == typeof(string))
                return "string";
            if (propType == typeof(Guid))
                return "guid";
            if (propType == typeof(byte[]))
                return "bytes";

            return "other";
        }

        private string GetDatabaseTypeFamily(string? dbType)
        {
            var type = (dbType ?? string.Empty).ToLowerInvariant();

            if (type.Contains("bigint") || type.Contains("int8") || type.Contains("bigserial"))
                return "bigint";
            if (type.Contains("smallint") || type.Contains("int2") || type.Contains("smallserial") || type.Contains("tinyint"))
                return "smallint";
            if (type.Contains("int") || type.Contains("integer") || type.Contains("serial"))
                return "int";
            if (type.Contains("decimal") || type.Contains("numeric") || type.Contains("money"))
                return "decimal";
            if (type.Contains("double"))
                return "double";
            if (type.Contains("float") || type.Contains("real"))
                return "float";
            if (type.Contains("bool") || type.Contains("bit"))
                return "bool";
            if (type.Contains("timestamp") || type.Contains("datetime") || type == "date" || type == "time")
                return "datetime";
            if (type.Contains("json"))
                return "json";
            if (type.Contains("char") || type.Contains("text") || type.Contains("json"))
                return "string";
            if (type.Contains("uuid") || type.Contains("guid") || type.Contains("uniqueidentifier"))
                return "guid";
            if (type.Contains("bytea") || type.Contains("blob") || type.Contains("binary"))
                return "bytes";

            return "other";
        }

        private List<string> GetTableDifferencesForSplit(Type entityType, string sampleTableName)
        {
            var differences = new List<string>();

            try
            {
                // 检查分表是否存在
                if (!_db!.DbMaintenance.IsAnyTable(sampleTableName, false))
                {
                    differences.Add($"+ 新建表: {sampleTableName}");
                    return differences;
                }

                // 检查表注释
                var tableInfo = _db.DbMaintenance.GetTableInfoList(false)
                    .FirstOrDefault(t => t.Name.Equals(sampleTableName, StringComparison.OrdinalIgnoreCase));
                var tableAttr = entityType.GetCustomAttribute<SugarTable>();
                var entityTableDescription = tableAttr?.TableDescription ?? "";
                var dbTableDescription = tableInfo?.Description ?? "";

                if (!string.Equals(entityTableDescription, dbTableDescription, StringComparison.Ordinal)
                    && !string.IsNullOrEmpty(entityTableDescription))
                {
                    differences.Add($"~ 表注释变更: \"{dbTableDescription}\" → \"{entityTableDescription}\"");
                }

                // 获取数据库中的列
                var dbColumns = _db.DbMaintenance.GetColumnInfosByTableName(sampleTableName, false);
                var dbColumnDict = dbColumns.ToDictionary(c => c.DbColumnName.ToLower(), c => c);
                var dbColumnNames = dbColumns.Select(c => c.DbColumnName.ToLower()).ToHashSet();

                // 获取实体中的属性
                var properties = entityType.GetProperties()
                    .Where(p => p.GetCustomAttribute<SugarColumn>()?.IsIgnore != true)
                    .ToList();

                foreach (var prop in properties)
                {
                    var columnAttr = prop.GetCustomAttribute<SugarColumn>();
                    var columnName = columnAttr?.ColumnName ?? prop.Name;
                    var columnNameLower = columnName.ToLower();

                    if (!dbColumnNames.Contains(columnNameLower))
                    {
                        differences.Add($"+ 新增字段: {columnName}");
                    }
                    else
                    {
                        var dbColumn = dbColumnDict[columnNameLower];

                        // 检查字段类型/长度/可空性变更
                        var columnDiffs = CompareColumnAttributes(prop, columnAttr, dbColumn);
                        differences.AddRange(columnDiffs);
                    }
                }

                // 检查是否有字段被删除
                var entityColumnNames = properties
                    .Select(p => (p.GetCustomAttribute<SugarColumn>()?.ColumnName ?? p.Name).ToLower())
                    .ToHashSet();

                foreach (var dbColumn in dbColumns)
                {
                    if (!entityColumnNames.Contains(dbColumn.DbColumnName.ToLower()))
                    {
                        differences.Add($"- 删除字段: {dbColumn.DbColumnName}");
                    }
                }

                // 检查索引差异
                var indexDifferences = GetIndexDifferences(entityType, sampleTableName);
                differences.AddRange(indexDifferences);
            }
            catch (Exception ex)
            {
                differences.Add($"! 分析失败: {ex.Message}");
            }

            return differences;
        }

        private List<int> ParseIndices(string input, int max)
        {
            return input.Split(',', '，')
                .Select(s => s.Trim())
                .Where(s => int.TryParse(s, out _))
                .Select(s => int.Parse(s))
                .Where(i => i > 0 && i <= max)
                .Distinct()
                .OrderBy(i => i)
                .ToList();
        }

        private bool ConfirmExecution()
        {
            if (_currentEnv!.RequireConfirmation)
            {
                Console.WriteLine();
                var confirmCode = GenerateConfirmCode(_currentEnv.ConfirmationPrefix);
                Console.Write($"请输入确认码 [{confirmCode}] 执行: ");
                var inputCode = Console.ReadLine()?.Trim();

                if (inputCode != confirmCode)
                {
                    PrintError("确认码错误，操作已取消");
                    return false;
                }
            }
            else
            {
                Console.WriteLine();
                Console.Write("确认执行？(y/n): ");
                var input = Console.ReadLine()?.Trim().ToLower();
                if (input != "y" && input != "yes")
                {
                    PrintInfo("操作已取消");
                    return false;
                }
            }

            return true;
        }

        private void WriteLog(string operation, List<string> entries)
        {
            try
            {
                var logDir = _configuration.LogPath;

                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var logFile = Path.Combine(logDir, $"codefirst_{DateTime.Now:yyyy-MM-dd_HHmmss}.log");
                var content = new List<string>
                {
                    $"操作时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    $"目标环境: {_currentEnvName} ({_currentEnv?.Description})",
                    $"操作类型: {operation}",
                    "",
                    "执行记录:",
                    "----------"
                };
                content.AddRange(entries);

                File.WriteAllLines(logFile, content);
                PrintInfo($"操作已记录到: {logFile}");
            }
            catch
            {
                // 日志写入失败不影响主流程
            }
        }

        #endregion

        #region 输出格式化

        private void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private void PrintWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private void PrintInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        #endregion
    }
}
