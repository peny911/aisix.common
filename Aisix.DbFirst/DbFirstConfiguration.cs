using Aisix.Common.Db;

namespace Aisix.DbFirst
{
    /// <summary>
    /// DbFirst 配置
    /// </summary>
    public class DbFirstConfiguration
    {
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// 数据库类型
        /// </summary>
        public DataBaseType DbType { get; set; } = DataBaseType.MySql;

        /// <summary>
        /// 解决方案名称（用于生成命名空间）
        /// </summary>
        public string SolutionName { get; set; } = string.Empty;

        /// <summary>
        /// 实体类输出目录
        /// </summary>
        public string EntityOutputPath { get; set; } = string.Empty;

        /// <summary>
        /// 服务实现输出目录
        /// </summary>
        public string ServiceOutputPath { get; set; } = string.Empty;

        /// <summary>
        /// 服务接口输出目录
        /// </summary>
        public string IServiceOutputPath { get; set; } = string.Empty;

        /// <summary>
        /// 实体命名空间（默认为 {SolutionName}.Interface.Entities）
        /// </summary>
        public string? EntityNamespace { get; set; }

        /// <summary>
        /// 服务命名空间（默认为 {SolutionName}.Interface）
        /// </summary>
        public string? ServiceNamespace { get; set; }

        /// <summary>
        /// 排除的表列表（支持通配符，如 "sys_*", "*_log"）
        /// </summary>
        public List<string> ExcludedTables { get; set; } = new();

        /// <summary>
        /// 是否启用交互模式（选择要生成的表）
        /// </summary>
        public bool InteractiveMode { get; set; } = true;

        /// <summary>
        /// 指定要生成的表列表（非交互模式下使用，为空则生成所有表）
        /// </summary>
        public List<string> Tables { get; set; } = new();

        /// <summary>
        /// 作者名称（用于生成文件头注释）
        /// </summary>
        public string Author { get; set; } = "Auto Generated";

        /// <summary>
        /// 服务模板中额外的 using 语句
        /// </summary>
        public List<string> AdditionalUsings { get; set; } = new();

        /// <summary>
        /// 获取实际的实体命名空间
        /// </summary>
        public string GetEntityNamespace() => EntityNamespace ?? $"{SolutionName}.Interface.Entities";

        /// <summary>
        /// 获取实际的服务命名空间
        /// </summary>
        public string GetServiceNamespace() => ServiceNamespace ?? $"{SolutionName}.Interface";
    }
}
