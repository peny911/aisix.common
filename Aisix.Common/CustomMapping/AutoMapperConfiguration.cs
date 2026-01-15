using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Aisix.Common.CustomMapping
{
    public static class AutoMapperConfiguration
    {
        public static void InitializeAutoMapper(this IServiceCollection services, params Assembly[] assemblies)
        {
            //With AutoMapper Instance, you need to call AddAutoMapper services and pass assemblies that contains automapper Profile class
            //services.AddAutoMapper(assembly1, assembly2, assembly3);
            //See http://docs.automapper.org/en/stable/Configuration.html
            //And https://code-maze.com/automapper-net-core/

            services.AddAutoMapper(config =>
            {
                config.AddCustomMappingProfile();
            }, assemblies);

            #region Deprecated (Use AutoMapper Instance instead)
            //Mapper.Initialize(config =>
            //{
            //    config.AddCustomMappingProfile();
            //});

            ////Compile mapping after configuration to boost map speed
            //Mapper.Configuration.CompileMappings();
            #endregion
        }

        public static void AddCustomMappingProfile(this IMapperConfigurationExpression config)
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                config.AddCustomMappingProfile(entryAssembly);
            }
        }

        public static void AddCustomMappingProfile(this IMapperConfigurationExpression config, params Assembly[] assemblies)
        {
            var allTypes = assemblies.SelectMany(a => a.ExportedTypes);

            // 筛选实现 IHaveCustomMapping 接口的具体类（非抽象类）
            // 通过反射创建实例，过滤 null，转为非空集合
            var list = allTypes
                .Where(type => type.IsClass && !type.IsAbstract &&
                    type.GetInterfaces().Contains(typeof(IHaveCustomMapping)))
                .Select(type => Activator.CreateInstance(type) as IHaveCustomMapping)
                .Where(instance => instance != null)
                .Cast<IHaveCustomMapping>();

            var profile = new CustomMappingProfile(list);

            config.AddProfile(profile);
        }
    }
}
