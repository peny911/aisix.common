using System.Security.Cryptography;
using System.Text;

namespace Aisix.Common.Utils
{
    public class Md5Helper
    {
        /// <summary>
        /// 生成MD5哈希
        /// </summary>
        /// <param name="input">要哈希的字符串</param>
        /// <param name="length">生成的哈希长度，16表示MD5的16位表示，32表示完整的32位表示</param>
        /// <param name="isUpperCase">是否要大写形式</param>
        /// <returns>生成的MD5哈希字符串</returns>
        public static string GenerateMd5(string input, int length = 32, bool isUpperCase = false)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                StringBuilder sBuilder = new StringBuilder();

                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                var result = sBuilder.ToString();
                if (length == 16)
                {
                    result = result.Substring(8, 16);
                }

                return isUpperCase ? result.ToUpper() : result;
            }
        }

    }
}
