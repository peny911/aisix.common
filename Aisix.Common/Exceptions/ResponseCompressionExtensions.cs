using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;

namespace Aisix.Common.Exceptions
{
    public static class ResponseCompressionExtensions
    {
        /// <summary>
        /// 添加并配置 Gzip 响应压缩
        /// </summary>
        public static IServiceCollection AddGzipCompression(this IServiceCollection services)
        {
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true; // 允许 HTTPS 下的压缩
                options.Providers.Add<GzipCompressionProvider>(); // 启用 Gzip 压缩

                // 指定需要压缩的 MIME 类型
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
                {
                    "application/json",
                    "application/xml",
                    "text/plain",
                    "text/html"
                });
            });

            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest; // 压缩级别，可选 Optimal, Fastest, NoCompression
            });

            return services;
        }

        /// <summary>
        /// 使用 Gzip 压缩中间件
        /// </summary>
        public static IApplicationBuilder UseGzipCompression(this IApplicationBuilder app)
        {
            return app.UseResponseCompression();
        }
    }
}
