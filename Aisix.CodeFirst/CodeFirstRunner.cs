using Aisix.CodeFirst.Services;

namespace Aisix.CodeFirst
{
    /// <summary>
    /// CodeFirst 工具运行器
    /// 提供静态方法用于快速启动 CodeFirst 功能
    /// </summary>
    public static class CodeFirstRunner
    {
        /// <summary>
        /// 运行 CodeFirst 工具
        /// </summary>
        /// <param name="configuration">CodeFirst 配置</param>
        public static void Run(CodeFirstConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (string.IsNullOrWhiteSpace(configuration.EntityAssembly))
                throw new ArgumentException("EntityAssembly 不能为空", nameof(configuration));

            if (string.IsNullOrWhiteSpace(configuration.EntityNamespace))
                throw new ArgumentException("EntityNamespace 不能为空", nameof(configuration));

            if (configuration.Environments == null || configuration.Environments.Count == 0)
                throw new ArgumentException("Environments 不能为空", nameof(configuration));

            var service = new CodeFirstService(configuration);
            service.Run();
        }
    }
}
