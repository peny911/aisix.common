using SqlSugar;

namespace Aisix.CodeFirst.Extensions
{
    /// <summary>
    /// SqlSugar 扩展方法
    /// </summary>
    public static class SqlSugarExtensions
    {
        /// <summary>
        /// 为 CodeFirst 工具创建简单的 SqlSugar 客户端
        /// </summary>
        public static ISqlSugarClient CreateSqlSugarClient(EnvironmentSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            return new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = settings.ConnectionString,
                DbType = (DbType)settings.DbType,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            });
        }
    }
}
