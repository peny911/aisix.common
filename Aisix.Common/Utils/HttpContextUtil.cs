using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Aisix.Common.Utils
{
    public static class HttpContextUtil
    {
        /// <summary>
        /// 获取客户端 IP 地址
        /// 优先从 X-Forwarded-For 头获取（适用于反向代理场景）
        /// </summary>
        /// <param name="context">HTTP 上下文</param>
        /// <returns>客户端 IP 地址，获取失败返回 null</returns>
        public static string? GetClientIpAddress(HttpContext context)
        {
            if (context == null) return null;

            // 优先从 X-Forwarded-For 获取（反向代理场景）
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // X-Forwarded-For 可能包含多个 IP，取第一个（原始客户端 IP）
                var ip = forwardedFor.Split(',')[0].Trim();
                if (IsIp(ip))
                {
                    return ip;
                }
            }

            // 尝试从 X-Real-IP 获取（某些反向代理使用此头）
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp) && IsIp(realIp))
            {
                return realIp;
            }

            // 直接从连接获取
            var remoteIp = context.Connection.RemoteIpAddress;
            if (remoteIp != null)
            {
                // 处理 IPv4 映射的 IPv6 地址
                if (remoteIp.IsIPv4MappedToIPv6)
                {
                    return remoteIp.MapToIPv4().ToString();
                }
                return remoteIp.ToString();
            }

            return null;
        }

        /// <summary>
        /// 获取客户端 IP 地址（HttpRequest 扩展方法）
        /// </summary>
        public static string? GetClientIpAddress(this HttpRequest request)
        {
            return GetClientIpAddress(request.HttpContext);
        }

        /// <summary>
        /// 获取当前服务器IP地址，如果没有获取到则返回空字符串
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            //throw new Exception("No network adapters with an IPv4 address in the system!");
            return string.Empty;
        }

        public static string GetLinuxIPAddress()
        {
            string interfaceName = "eth0";
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var ni in networkInterfaces)
            {
                if (ni.Name == interfaceName)
                {
                    var ipProperties = ni.GetIPProperties();
                    foreach (var ip in ipProperties.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }

            return string.Empty;
        }

        public static bool IsIp(string ip)
        {
            return Regex.IsMatch(ip, "^((2[0-4]\\d|25[0-5]|[01]?\\d\\d?)\\.){3}(2[0-4]\\d|25[0-5]|[01]?\\d\\d?)$");
        }
    }
}
