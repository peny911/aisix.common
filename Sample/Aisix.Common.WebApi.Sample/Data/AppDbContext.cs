using Aisix.Common.WebApi.Sample.Models;
using SqlSugar;

namespace Aisix.Common.WebApi.Sample.Data
{
    /// <summary>
    /// 应用程序数据库上下文
    /// </summary>
    public class AppDbContext
    {
        private readonly ISqlSugarClient _db;

        public AppDbContext(ISqlSugarClient db)
        {
            _db = db;
        }

        /// <summary>
        /// 初始化数据库表结构
        /// </summary>
        public void InitializeDatabase()
        {
            // 创建 test 表（如果不存在）
            _db.CodeFirst.SetStringDefaultLength(200).InitTables<TestEntity>();
        }
    }
}