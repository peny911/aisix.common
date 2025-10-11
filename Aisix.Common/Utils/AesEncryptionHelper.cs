using System.Security.Cryptography;
using System.Text;

namespace Aisix.Common.Utils
{
    public static class AesEncryptionHelper
    {
        public static string EncryptString(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentException("Plain text cannot be null or empty.", nameof(plainText));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (key.Length < 16)
                throw new ArgumentException("Key must be at least 16 characters long.", nameof(key));

            byte[] array;

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aes.GenerateIV();
                byte[] iv = aes.IV;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    memoryStream.Write(iv, 0, iv.Length);
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }
                    }
                    array = memoryStream.ToArray();
                }
            }

            return ByteArrayToHexString(array);
        }

        public static string DecryptString(string cipherTextHex, string key)
        {
            if (string.IsNullOrEmpty(cipherTextHex))
                throw new ArgumentException("Cipher text cannot be null or empty.", nameof(cipherTextHex));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            if (key.Length < 16)
                throw new ArgumentException("Key must be at least 16 characters long.", nameof(key));

            byte[] buffer = HexStringToByteArray(cipherTextHex);
            
            if (buffer.Length < 16)
                throw new ArgumentException("Invalid cipher text length.", nameof(cipherTextHex));

            byte[] iv = new byte[16];
            byte[] cipherText = new byte[buffer.Length - 16];
            Buffer.BlockCopy(buffer, 0, iv, 0, 16);
            Buffer.BlockCopy(buffer, 16, cipherText, 0, cipherText.Length);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(cipherText))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }

        private static string ByteArrayToHexString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        private static byte[] HexStringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }
    }

}