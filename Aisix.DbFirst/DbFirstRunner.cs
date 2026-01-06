using Aisix.DbFirst.Services;
using SqlSugar;

namespace Aisix.DbFirst
{
    /// <summary>
    /// DbFirst 工具运行器
    /// 提供静态方法用于快速启动 DbFirst 代码生成功能
    /// </summary>
    public static class DbFirstRunner
    {
        /// <summary>
        /// 运行 DbFirst 代码生成工具
        /// </summary>
        /// <param name="configuration">DbFirst 配置</param>
        public static void Run(DbFirstConfiguration configuration)
        {
            ValidateConfiguration(configuration);

            var db = CreateSqlSugarClient(configuration);
            var service = new DbFirstService(db, configuration);
            service.Run();
        }

        /// <summary>
        /// 运行 DbFirst 代码生成工具（使用已有的 SqlSugar 客户端）
        /// </summary>
        /// <param name="db">SqlSugar 客户端</param>
        /// <param name="configuration">DbFirst 配置</param>
        public static void Run(ISqlSugarClient db, DbFirstConfiguration configuration)
        {
            if (db == null)
                throw new ArgumentNullException(nameof(db));

            ValidateConfiguration(configuration);

            var service = new DbFirstService(db, configuration);
            service.Run();
        }

        /// <summary>
        /// 获取所有可用的表
        /// </summary>
        /// <param name="configuration">DbFirst 配置</param>
        /// <returns>表名列表</returns>
        public static List<string> GetAllTables(DbFirstConfiguration configuration)
        {
            ValidateConfiguration(configuration);

            var db = CreateSqlSugarClient(configuration);
            var service = new DbFirstService(db, configuration);
            return service.GetAllTables();
        }

        /// <summary>
        /// 生成指定表的代码
        /// </summary>
        /// <param name="configuration">DbFirst 配置</param>
        /// <param name="tableNames">表名列表</param>
        /// <returns>生成结果列表</returns>
        public static List<GenerateResult> GenerateTables(DbFirstConfiguration configuration, List<string> tableNames)
        {
            ValidateConfiguration(configuration);

            var db = CreateSqlSugarClient(configuration);
            var service = new DbFirstService(db, configuration);
            return service.GenerateTables(tableNames);
        }

        /// <summary>
        /// 生成所有表的代码（非交互模式）
        /// </summary>
        /// <param name="configuration">DbFirst 配置</param>
        /// <returns>生成结果列表</returns>
        public static List<GenerateResult> GenerateAllTables(DbFirstConfiguration configuration)
        {
            ValidateConfiguration(configuration);

            var db = CreateSqlSugarClient(configuration);
            var service = new DbFirstService(db, configuration);
            var allTables = service.GetAllTables();
            return service.GenerateTables(allTables);
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        private static void ValidateConfiguration(DbFirstConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (string.IsNullOrWhiteSpace(configuration.ConnectionString))
                throw new ArgumentException("ConnectionString 不能为空", nameof(configuration));

            if (string.IsNullOrWhiteSpace(configuration.SolutionName))
                throw new ArgumentException("SolutionName 不能为空", nameof(configuration));

            if (string.IsNullOrWhiteSpace(configuration.EntityOutputPath))
                throw new ArgumentException("EntityOutputPath 不能为空", nameof(configuration));

            if (string.IsNullOrWhiteSpace(configuration.ServiceOutputPath))
                throw new ArgumentException("ServiceOutputPath 不能为空", nameof(configuration));

            if (string.IsNullOrWhiteSpace(configuration.IServiceOutputPath))
                throw new ArgumentException("IServiceOutputPath 不能为空", nameof(configuration));
        }

        /// <summary>
        /// 创建 SqlSugar 客户端
        /// </summary>
        private static ISqlSugarClient CreateSqlSugarClient(DbFirstConfiguration configuration)
        {
            return new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = configuration.ConnectionString,
                DbType = (DbType)(int)configuration.DbType,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
        }
    }
}
