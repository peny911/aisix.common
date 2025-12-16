using Aisix.Common.Db;

namespace Aisix.CodeFirst
{
    /// <summary>
    /// 环境配置
    /// </summary>
    public class EnvironmentSettings
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
        /// 是否允许删除字段(生产环境建议设为 false)
        /// </summary>
        public bool AllowDeleteColumn { get; set; } = true;

        /// <summary>
        /// 是否需要确认码验证
        /// </summary>
        public bool RequireConfirmation { get; set; } = false;

        /// <summary>
        /// 确认码前缀
        /// </summary>
        public string ConfirmationPrefix { get; set; } = string.Empty;

        /// <summary>
        /// 环境描述
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// CodeFirst 配置
    /// </summary>
    public class CodeFirstConfiguration
    {
        /// <summary>
        /// 实体程序集名称
        /// </summary>
        public string EntityAssembly { get; set; } = string.Empty;

        /// <summary>
        /// 实体命名空间
        /// </summary>
        public string EntityNamespace { get; set; } = string.Empty;

        /// <summary>
        /// 日志路径
        /// </summary>
        public string LogPath { get; set; } = "logs";

        /// <summary>
        /// 环境配置字典
        /// </summary>
        public Dictionary<string, EnvironmentSettings> Environments { get; set; } = new();
    }
}
