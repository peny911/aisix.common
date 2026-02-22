using System.Reflection;
using Aisix.CodeFirst.Extensions;
using SqlSugar;

namespace Aisix.CodeFirst.Services
{
    public class CodeFirstService : ICodeFirstService
    {
        private readonly CodeFirstConfiguration _configuration;
        private readonly Dictionary<string, EnvironmentSettings> _environments;

        private ISqlSugarClient? _db;
        private EnvironmentSettings? _currentEnv;
        private string _currentEnvName = string.Empty;

        // 实体分类缓存
        private List<Type> _normalEntities = new();
        private List<Type> _splitEntities = new();

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

                Console.WriteLine($"发现 {allTypes.Count} 个实体类：");
                Console.WriteLine($"  [普通表] 共 {_normalEntities.Count} 个");
                Console.WriteLine($"  [分表模板] 共 {_splitEntities.Count} 个 (标记 [SplitTable])");
                Console.WriteLine();
                Console.WriteLine("注：分表模板也可在「更新普通表结构」中更新其基础表");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                PrintError($"加载实体失败: {ex.Message}");
            }
        }

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

                    // 创建索引（从实体特性中获取所有索引定义）
                    CreateIndexesFromEntity(tableName, entity);

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

                    Console.WriteLine($"  [{tableName}] 正在更新 {splitTables.Count} 张分表...");

                    // 使用 SplitTables().InitTables 更新所有分表
                    _db!.CodeFirst.SplitTables().InitTables(entity);

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

                // 找出新增的字段及其应该在的位置
                string? previousColumn = null;
                foreach (var prop in properties)
                {
                    var columnAttr = prop.GetCustomAttribute<SugarColumn>();
                    var columnName = columnAttr?.ColumnName ?? prop.Name;

                    if (!dbColumnDict.ContainsKey(columnName.ToLower()))
                    {
                        // 这是新字段，生成 ALTER TABLE ADD COLUMN ... AFTER ...
                        var columnDef = GenerateColumnDefinition(prop, columnAttr);

                        foreach (var tableName in tableNames)
                        {
                            var afterClause = previousColumn != null ? $" AFTER `{previousColumn}`" : " FIRST";
                            sqlList.Add($"ALTER TABLE `{tableName}` ADD COLUMN {columnDef}{afterClause};");
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
                            sqlList.Add($"-- [危险] ALTER TABLE `{tableName}` DROP COLUMN `{dbColumn.DbColumnName}`;");
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

        private string GenerateColumnDefinition(PropertyInfo prop, SugarColumn? columnAttr)
        {
            var columnName = columnAttr?.ColumnName ?? prop.Name;
            var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            var isNullable = columnAttr?.IsNullable ?? Nullable.GetUnderlyingType(prop.PropertyType) != null;

            // 确定 MySQL 数据类型
            string dataType;
            if (!string.IsNullOrEmpty(columnAttr?.ColumnDataType))
            {
                dataType = columnAttr.ColumnDataType;
            }
            else
            {
                dataType = GetMySqlDataType(propType, columnAttr);
            }

            // 构建列定义
            var sb = new System.Text.StringBuilder();
            sb.Append($"`{columnName}` {dataType}");

            if (!isNullable)
            {
                sb.Append(" NOT NULL");
            }

            if (!string.IsNullOrEmpty(columnAttr?.DefaultValue))
            {
                var defaultValue = columnAttr.DefaultValue;
                // 数字类型不需要引号
                if (propType == typeof(int) || propType == typeof(long) || propType == typeof(decimal) ||
                    propType == typeof(float) || propType == typeof(double) || propType == typeof(byte) ||
                    propType == typeof(short))
                {
                    sb.Append($" DEFAULT {defaultValue}");
                }
                else
                {
                    sb.Append($" DEFAULT '{defaultValue}'");
                }
            }
            else if (!isNullable)
            {
                // 非空字段需要默认值
                if (propType == typeof(int) || propType == typeof(long) || propType == typeof(byte) || propType == typeof(short))
                {
                    sb.Append(" DEFAULT 0");
                }
                else if (propType == typeof(decimal) || propType == typeof(float) || propType == typeof(double))
                {
                    sb.Append(" DEFAULT 0");
                }
                else if (propType == typeof(string))
                {
                    sb.Append(" DEFAULT ''");
                }
                else if (propType == typeof(bool))
                {
                    sb.Append(" DEFAULT 0");
                }
            }

            // 添加注释
            var description = columnAttr?.ColumnDescription;
            if (!string.IsNullOrEmpty(description))
            {
                sb.Append($" COMMENT '{description.Replace("'", "\\'")}'");
            }

            return sb.ToString();
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

            return "varchar(255)";
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
            // 尝试从 XML 注释获取描述，这里简化处理
            return "";
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
                        // 检查字段注释
                        var dbColumn = dbColumnDict[columnNameLower];
                        var entityDescription = (columnAttr?.ColumnDescription ?? "").Trim();
                        var dbDescription = (dbColumn.ColumnDescription ?? "").Trim();

                        if (!string.Equals(entityDescription, dbDescription, StringComparison.Ordinal)
                            && !string.IsNullOrEmpty(entityDescription))
                        {
                            differences.Add($"~ 字段注释变更 [{columnName}]: \"{TruncateString(dbDescription, 20)}\" → \"{TruncateString(entityDescription, 20)}\"");
                        }
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
                    .Select(name => name.ToLower())
                    .ToHashSet();

                // 检查新增的索引
                foreach (var indexName in entityIndexNames)
                {
                    if (!string.IsNullOrEmpty(indexName) && !dbIndexNames.Contains(indexName.ToLower()))
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

        private void CreateIndex(string tableName, Type entityType, string indexName)
        {
            try
            {
                // 从实体特性中获取索引定义的字段
                var attrs = entityType.GetCustomAttributesData();
                foreach (var attr in attrs)
                {
                    if (attr.AttributeType.Name == "SugarIndexAttribute")
                    {
                        // 获取构造函数参数：IndexName, 字段名1, 字段名2, ...
                        if (attr.ConstructorArguments.Count > 1)
                        {
                            var name = attr.ConstructorArguments[0].Value?.ToString() ?? "";
                            if (name == indexName)
                            {
                                // 获取字段名（从第1个参数开始）
                                var fieldNames = new List<string>();
                                for (int i = 1; i < attr.ConstructorArguments.Count; i++)
                                {
                                    var fieldValue = attr.ConstructorArguments[i].Value;
                                    if (fieldValue != null)
                                    {
                                        fieldNames.Add(fieldValue.ToString()!);
                                    }
                                }

                                if (fieldNames.Count > 0)
                                {
                                    var sql = $"ALTER TABLE `{tableName}` ADD INDEX `{indexName}` (`{string.Join("`, `", fieldNames)}`)";
                                    _db!.Ado.ExecuteCommand(sql);
                                    Console.WriteLine($"    创建索引: {indexName} on ({string.Join(", ", fieldNames)})");
                                }
                                break;
                            }
                        }
                    }
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
                // 获取数据库中已有的索引
                var dbIndexes = _db!.DbMaintenance.GetIndexList(tableName)
                    .Select(i => i.ToLower())
                    .ToHashSet();

                // 从实体特性中获取所有索引定义
                var attrs = entityType.GetCustomAttributesData();
                foreach (var attr in attrs)
                {
                    if (attr.AttributeType.Name == "SugarIndexAttribute")
                    {
                        // 获取构造函数参数：IndexName, 字段名1, 字段名2, ..., OrderByType
                        if (attr.ConstructorArguments.Count >= 2)
                        {
                            // 第0个是索引名
                            var indexName = attr.ConstructorArguments[0].Value?.ToString() ?? "";
                            if (string.IsNullOrEmpty(indexName)) continue;

                            // 检查索引是否已存在
                            if (dbIndexes.Contains(indexName.ToLower()))
                            {
                                Console.WriteLine($"    索引已存在: {indexName}");
                                continue;
                            }

                            // 获取字段名（从第1个参数开始，跳过最后两个：OrderByType 和 isUnique）
                            var fieldNames = new List<string>();
                            // 字段名是参数1开始，到倒数第3个为止（倒数第2个是OrderByType，倒数第1个是isUnique）
                            var lastFieldIndex = attr.ConstructorArguments.Count - 3;
                            for (int i = 1; i <= lastFieldIndex; i++)
                            {
                                var arg = attr.ConstructorArguments[i];
                                var fieldValue = arg.Value;
                                if (fieldValue != null)
                                {
                                    fieldNames.Add(fieldValue.ToString()!);
                                }
                            }

                            if (fieldNames.Count > 0)
                            {
                                var sql = $"ALTER TABLE `{tableName}` ADD INDEX `{indexName}` (`{string.Join("`, `", fieldNames)}`)";
                                _db.Ado.ExecuteCommand(sql);
                                Console.WriteLine($"    创建索引: {indexName} on ({string.Join(", ", fieldNames)})");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    创建索引失败: {ex.Message}");
            }
        }

        private string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
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
                        // 检查字段注释
                        var dbColumn = dbColumnDict[columnNameLower];
                        var entityDescription = (columnAttr?.ColumnDescription ?? "").Trim();
                        var dbDescription = (dbColumn.ColumnDescription ?? "").Trim();

                        if (!string.Equals(entityDescription, dbDescription, StringComparison.Ordinal)
                            && !string.IsNullOrEmpty(entityDescription))
                        {
                            differences.Add($"~ 字段注释变更 [{columnName}]: \"{TruncateString(dbDescription, 20)}\" → \"{TruncateString(entityDescription, 20)}\"");
                        }
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
