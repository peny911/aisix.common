using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Web;

namespace Aisix.Common.Utils
{
    public class NetUtil
    {
        /// <summary>
        /// 获取本机IPv4地址
        /// </summary>
        /// <returns></returns>
        public static IPAddress? GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            //throw new Exception("No network adapters with an IPv4 address in the system!");
            return null;
        }

        /// <summary>
        /// 获取本机IPv4地址
        /// </summary>
        /// <param name="networkInterfaceName">网卡名称</param>
        /// <returns></returns>
        public static IPAddress? GetLocalIPAddress(string networkInterfaceName)
        {
            foreach (var networkInterface in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.Name == networkInterfaceName)
                {
                    var ipProperties = networkInterface.GetIPProperties();
                    foreach (var ip in ipProperties.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address;
                        }
                    }
                }
            }

            //throw new Exception("Local IP Address Not Found!");
            return null;
        }

        public static string ToUrlParams(object obj)
        {
            return string.Join("&", obj.GetType().GetProperties()
                .Where(x => x.GetValue(obj) != null)
                // 过滤掉带有[JsonIgnore]特性的属性
                .Where(x => !Attribute.IsDefined(x, typeof(JsonIgnoreAttribute)))
                .Select(x => $"{x.Name}={HttpUtility.UrlEncode(x.GetValue(obj)!.ToString())}"));
        }
    }
}
