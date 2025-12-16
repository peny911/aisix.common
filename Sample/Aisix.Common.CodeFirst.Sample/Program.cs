using Aisix.CodeFirst;
using Aisix.Common.Db;

namespace Aisix.Common.CodeFirst.Sample
{
    /// <summary>
    /// Aisix.CodeFirst 使用示例
    /// 为 Aisix.Common.WebApi.Sample 项目提供 CodeFirst 功能
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("============================================================");
            Console.WriteLine("  Aisix.CodeFirst 使用示例");
            Console.WriteLine("  为 Aisix.Common.WebApi.Sample 提供 CodeFirst 功能");
            Console.WriteLine("============================================================");
            Console.WriteLine();

            // 配置 CodeFirst
            var config = new CodeFirstConfiguration
            {
                // 指定实体所在的程序集和命名空间
                // WebApi.Sample 的实体在 Aisix.Common.WebApi.Sample.Models 命名空间
                EntityAssembly = "Aisix.Common.WebApi.Sample",
                EntityNamespace = "Aisix.Common.WebApi.Sample.Models",

                // 日志路径
                LogPath = "logs",

                // 环境配置
                Environments = new Dictionary<string, EnvironmentSettings>
                {
                    // 开发环境
                    ["Development"] = new EnvironmentSettings
                    {
                        ConnectionString = "server=your-dev-server;port=3306;database=your_database;uid=root;pwd=YourPassword;AllowLoadLocalInfile=true;",
                        DbType = DataBaseType.MySql,
                        AllowDeleteColumn = true,
                        RequireConfirmation = false,
                        Description = "开发环境 (本地数据库)"
                    },

                    // 生产环境示例
                    ["Production"] = new EnvironmentSettings
                    {
                        ConnectionString = "Server=your-prod-server;Port=3306;Database=aisix_core_prod;Uid=root;Pwd=YourPassword;",
                        DbType = DataBaseType.MySql,
                        AllowDeleteColumn = false,
                        RequireConfirmation = true,
                        ConfirmationPrefix = "PROD",
                        Description = "生产环境 (需要确认码)"
                    }
                }
            };

            // 运行 CodeFirst 工具
            CodeFirstRunner.Run(config);
        }
    }
}
