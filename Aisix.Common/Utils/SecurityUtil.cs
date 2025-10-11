using System.Security.Cryptography;
using System.Text;

namespace Aisix.Common.Utils
{
    public class SecurityUtil
    {
        public static string Sha256EncryptString(string str)
        {
            if (string.IsNullOrEmpty(str))
                throw new ArgumentException("String cannot be null or empty.", nameof(str));

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(str));
                StringBuilder stringBuilder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    stringBuilder.Append(b.ToString("x2"));
                }
                return stringBuilder.ToString();
            }
        }

        [Obsolete("MD5 is not secure. Use Sha256EncryptString instead.")]
        public static string Md5EncryptString(string str)
        {
            if (string.IsNullOrEmpty(str))
                throw new ArgumentException("String cannot be null or empty.", nameof(str));

            using (MD5 mD = MD5.Create())
            {
                byte[] array = mD.ComputeHash(Encoding.UTF8.GetBytes(str));
                StringBuilder stringBuilder = new StringBuilder();
                foreach (byte b in array)
                {
                    stringBuilder.Append(b.ToString("x2"));
                }
                return stringBuilder.ToString();
            }
        }

        public static string ToBase64String(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return Convert.ToBase64String(bytes);
        }

        public static string Base64ToString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            byte[] bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
