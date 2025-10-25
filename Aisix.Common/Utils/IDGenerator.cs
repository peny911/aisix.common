using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace Aisix.Common.Utils
{
    public static class IDGenerator
    {
        public static string GenerateGUID()
        {
            return Guid.NewGuid().ToString();
        }

        public static string GenerateCUID()
        {
            return CuidGenerator.Generate();
        }

        public static string GetRandomString(int length, bool includeLetters = true, bool includeUppercase = true, bool includeLowercase = true, bool includeNumbers = true, bool includeSpecialChars = false)
        {
            StringBuilder charPool = new StringBuilder();

            if (includeLetters)
            {
                if (includeUppercase)
                {
                    charPool.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
                }

                if (includeLowercase)
                {
                    charPool.Append("abcdefghijklmnopqrstuvwxyz");
                }
            }

            if (includeNumbers)
            {
                charPool.Append("0123456789");
            }

            if (includeSpecialChars)
            {
                charPool.Append("!@#$%^&*()-_=+[{]};:'\",<.>/?\\|");
            }

            if (charPool.Length == 0)
            {
                throw new ArgumentException("Must specify a valid character set to include.");
            }

            var chars = charPool.ToString().ToCharArray();
            var random = Random.Shared;

            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        //// <summary>
        /// 根据服务器的 MAC 地址和当前进程 ID 生成唯一的 WorkId
        /// </summary>
        /// <param name="maxWorkId">最大允许的 WorkId 值（例如 31 表示 5 位）</param>
        /// <returns>生成的 WorkId</returns>
        public static int GetUniqueWorkId(int maxWorkId = 31)
        {
            // 获取所有在线且非回环接口的 MAC 地址
            var macAddress = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                              nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault(mac => !string.IsNullOrEmpty(mac));

            // 如果无法获取有效的 MAC 地址，使用机器名称作为备用方案
            if (string.IsNullOrEmpty(macAddress))
            {
                macAddress = Environment.MachineName;
            }

            // 获取当前进程的 ID 作为区分同机不同服务的标识
            int processId = Process.GetCurrentProcess().Id;

            // 如果需要，还可以加入其他唯一因素，例如监听的端口号或服务名称
            // string servicePort = "5000"; // 示例：端口号
            // string combined = $"{macAddress}-{processId}-{servicePort}";

            // 这里组合 MAC 地址和进程 ID
            string combined = $"{macAddress}-{processId}";

            // 使用 SHA256 对组合后的字符串进行哈希处理
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                // 取 hash 前 4 个字节转换为整数
                int intHash = BitConverter.ToInt32(hash, 0);
                // 取绝对值后进行取模运算，确保 WorkId 在 0 ~ maxWorkId 范围内
                int workId = Math.Abs(intHash) % (maxWorkId + 1);
                return workId;
            }
        }
    }
}

