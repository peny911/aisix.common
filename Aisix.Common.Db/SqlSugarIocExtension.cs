using Aisix.Common.Db.Repository;
using Aisix.Common.Db.Service;
using Aisix.Common.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using SqlSugar.IOC;

namespace Aisix.Common.Db
{
    public static class SqlSugarIocExtension
    {
        /// <summary>
        /// SqlSugar.IOC注入
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public static void AddSqlSugarIocSetup(this IServiceCollection services, IConfiguration configuration)
        {
            var dBOptions = configuration.GetSection("DBS").Get<DBOptions>();

            if (dBOptions == null)
            {
                throw new ArgumentNullException("请在appsettings.json中配置DBS节点");
            }

            var allCon = dBOptions.MutiDBConns.Where(i => i.Enabled).ToList();
            var listConfig = new List<IocConfig>();

            allCon.ForEach(q =>
            {
                listConfig.Add(new IocConfig()
                {
                    DbType = (IocDbType)q.DbType,
                    ConnectionString = q.Connection,
                    IsAutoCloseConnection = true,
                    ConfigId = q.ConnId,
                });
            });

            SugarIocServices.AddSqlSugar(listConfig);

            if (dBOptions.ConsoleSql)
            {
                SugarIocServices.ConfigurationSugar(db =>
                {
                    allCon.ForEach(q =>
                    {
                        db.GetConnectionScope(q.ConnId).Aop.OnLogExecuting = (sql, p) =>
                        {
                            Console.WriteLine($"\n-----\n{sql}\n"); // 输出sql
                            if (p != null)
                            {
                                Console.WriteLine(string.Join(",", p.Select(it => it.ParameterName + ":" + (it.Value ?? "null")))); // 参数
                            }
                        };
                    });
                });
            }

            services.AddScoped<ISqlSugarClient>(sp => DbScoped.SugarScope);
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddScoped(typeof(IBaseService<>), typeof(BaseService<>));
            services.AddScoped<UnitOfWork.IUnitOfWork, UnitOfWork.UnitOfWork>();

            SnowFlakeSingle.WorkId = IDGenerator.GetUniqueWorkId();
        }
    }
}
