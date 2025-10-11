using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aisix.Common.Wxe
{
    public static class WxeExtension
    {
        public static void AddWxeService(this IServiceCollection services)
        {
            services.AddHttpClient("wxe", c =>
            {
                c.BaseAddress = new Uri("http://wxe.appinai.com/api/message");
                c.DefaultRequestHeaders.Add("Accept", "application/json");
                c.DefaultRequestHeaders.Add("token", "LEoIdd0Ovnbnv4x4Kevq8EJdBxaDQWUW");
            });

            services.AddSingleton<IWxeMessager, WxeMessager>();
        }
    }
}
