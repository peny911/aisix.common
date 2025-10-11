using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Aisix.Common.Utils
{
    public static class HttpContextUtil
    {
        private const string HttpContextKey = "MS_HttpContext";

        private const string RemoteEndpointMessage = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";

        public static string GetClientIpAddress(this HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                dynamic val = request.Properties["MS_HttpContext"];
                if (val != null)
                {
                    return val.Request.UserHostAddress;
                }
            }

            if (request.Properties.ContainsKey("System.ServiceModel.Channels.RemoteEndpointMessageProperty"))
            {
                dynamic val2 = request.Properties["System.ServiceModel.Channels.RemoteEndpointMessageProperty"];
                if (val2 != null)
                {
                    return val2.Address;
                }
            }

            return null;
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
