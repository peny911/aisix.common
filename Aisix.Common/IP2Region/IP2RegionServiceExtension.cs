using IP2Region.Net.XDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aisix.Common.IP2Region
{
    public static class IP2RegionServiceExtension
    {
        public static void AddIP2RegionService(this IServiceCollection services, IConfiguration configuration)
        {
            var xdbFilePath = configuration["IP2Region:XdbFilePath"];
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrEmpty(xdbFilePath)) throw new ArgumentNullException(nameof(xdbFilePath));
            services.AddSingleton<IIP2RegionSearcher>(new IP2RegionSearcher(xdbFilePath, CachePolicy.Content));
        }
    }
}
