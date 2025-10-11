using System;
using Xunit;
using Aisix.Common.Utils;
using Assert = Xunit.Assert;

namespace Aisix.Common.Tests.Utils
{
    /// <summary>
    /// AesEncryptionHelper 单元测试类
    /// 
    /// 测试范围：
    /// 1. 基本加密解密功能
    /// 2. 参数验证和错误处理
    /// 3. 不同输入类型的处理
    /// 4. 密钥长度和有效性验证
    /// 5. 随机IV生成机制
    /// 6. 安全性相关测试
    /// </summary>
    public class AesEncryptionHelperTests
    {
        /// <summary>
        /// 测试加密方法的基本功能
        /// 验证：对于有效的明文和密钥，加密方法应该返回非空的加密字符串
        /// </summary>
        [Fact]
        public void EncryptString_ValidInput_ReturnsEncryptedString()
        {
            // Arrange
            string plainText = "Hello, World!";
            string key = "ThisIsASecretKey123456789012";

            // Act
            string encryptedText = AesEncryptionHelper.EncryptString(plainText, key);

            // Assert
            Assert.NotNull(encryptedText);
            Assert.NotEmpty(encryptedText);
            Assert.NotEqual(plainText, encryptedText);
        }

        /// <summary>
        /// 测试解密方法的基本功能
        /// 验证：对于有效的加密文本和密钥，解密方法应该返回原始明文
        /// </summary>
        [Fact]
        public void DecryptString_ValidInput_ReturnsOriginalText()
        {
            // Arrange
            string plainText = "Hello, World!";
            string key = "ThisIsASecretKey123456789012";
            string encryptedText = AesEncryptionHelper.EncryptString(plainText, key);

            // Act
            string decryptedText = AesEncryptionHelper.DecryptString(encryptedText, key);

            // Assert
            Assert.Equal(plainText, decryptedText);
        }

        /// <summary>
        /// 测试加密解密的完整流程
        /// 验证：对于各种类型的输入（空格、普通文本、特殊字符、Unicode），
        /// 加密后解密应该能够完美还原原始文本
        /// </summary>
        /// <param name="plainText">要测试的明文</param>
        [Theory]
        [InlineData(" ")]
        [InlineData("test")]
        [InlineData("Hello, World! 123")]
        [InlineData("Special chars: !@#$%^&*()")]
        [InlineData("Unicode: 你好世界")]
        public void EncryptDecrypt_RoundTrip_ReturnsOriginalText(string plainText)
        {
            // Arrange
            string key = "ThisIsASecretKey123456789012";

            // Act
            string encryptedText = AesEncryptionHelper.EncryptString(plainText, key);
            string decryptedText = AesEncryptionHelper.DecryptString(encryptedText, key);

            // Assert
            Assert.Equal(plainText, decryptedText);
        }

        /// <summary>
        /// 测试长文本的加密解密
        /// 验证：对于1000+字符的长文本，加密解密流程应该正常工作
        /// </summary>
        [Fact]
        public void EncryptDecrypt_LongText_ReturnsOriginalText()
        {
            // Arrange
            string plainText = "Long text: " + new string('A', 1000);
            string key = "ThisIsASecretKey123456789012";

            // Act
            string encryptedText = AesEncryptionHelper.EncryptString(plainText, key);
            string decryptedText = AesEncryptionHelper.DecryptString(encryptedText, key);

            // Assert
            Assert.Equal(plainText, decryptedText);
        }

        /// <summary>
        /// 测试不同长度密钥的支持
        /// 验证：对于符合长度要求的密钥（≥16字符），加密解密应该正常工作
        /// </summary>
        /// <param name="key">要测试的密钥</param>
        [Theory]
        [InlineData("ThisIsASecretKey123456789012")]
        [InlineData("ThisIsAVeryLongSecretKeyThatIsMuchLongerThan32Characters")]
        public void EncryptString_DifferentKeyLengths_WorksCorrectly(string key)
        {
            // Arrange
            string plainText = "Hello, World!";

            // Act
            string encryptedText = AesEncryptionHelper.EncryptString(plainText, key);
            string decryptedText = AesEncryptionHelper.DecryptString(encryptedText, key);

            // Assert
            Assert.Equal(plainText, decryptedText);
        }

        /// <summary>
        /// 测试加密方法的空明文参数验证
        /// 验证：当明文为null时，应该抛出ArgumentException
        /// </summary>
        [Fact]
        public void EncryptString_NullPlainText_ThrowsArgumentException()
        {
            // Arrange
            string plainText = null;
            string key = "ThisIsASecretKey123456789012";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => AesEncryptionHelper.EncryptString(plainText, key));
        }

        /// <summary>
        /// 测试加密方法的空明文参数验证
        /// 验证：当明文为空字符串时，应该抛出ArgumentException
        /// </summary>
        [Fact]
        public void EncryptString_EmptyPlainText_ThrowsArgumentException()
        {
            // Arrange
            string plainText = "";
            string key = "ThisIsASecretKey123456789012";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => AesEncryptionHelper.EncryptString(plainText, key));
        }

        /// <summary>
        /// 测试加密方法的空密钥参数验证
        /// 验证：当密钥为null时，应该抛出ArgumentException
        /// </summary>
        [Fact]
        public void EncryptString_NullKey_ThrowsArgumentException()
        {
            // Arrange
            string plainText = "Hello, World!";
            string key = null;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => AesEncryptionHelper.EncryptString(plainText, key));
        }

        /// <summary>
        /// 测试加密方法的空密钥参数验证
        /// 验证：当密钥为空字符串时，应该抛出ArgumentException
        /// </summary>
        [Fact]
        public void EncryptString_EmptyKey_ThrowsArgumentException()
        {
            // Arrange
            string plainText = "Hello, World!";
            string key = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => AesEncryptionHelper.EncryptString(plainText, key));
        }

        /// <summary>
        /// 测试加密方法的密钥长度验证
        /// 验证：当密钥长度小于16字符时，应该抛出ArgumentException
        /// </summary>
        [Fact]
        public void EncryptString_ShortKey_ThrowsArgumentException()
        {
            // Arrange
            string plainText = "Hello, World!";
            string key = "ShortKey";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => AesEncryptionHelper.EncryptString(plainText, key));
        }

        /// <summary>
        /// 测试解密方法的空密文参数验证
        /// 验证：当密文为null时，应该抛出ArgumentException
        /// </summary>
        [Fact]
        public void DecryptString_NullCipherText_ThrowsArgumentException()
        {
            // Arrange
            string cipherText = null;
            string key = "ThisIsASecretKey123456789012";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => AesEncryptionHelper.DecryptString(cipherText, key));
        }

        /// <summary>
        /// 测试解密方法的空密文参数验证
        /// 验证：当密文为空字符串时，应该抛出ArgumentException
        /// </summary>
        [Fact]
        public void DecryptString_EmptyCipherText_ThrowsArgumentException()
        {
            // Arrange
            string cipherText = "";
            string key = "ThisIsASecretKey123456789012";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => AesEncryptionHelper.DecryptString(cipherText, key));
        }

        /// <summary>
        /// 测试解密方法的空密钥参数验证
        /// 验证：当密钥为null时，应该抛出ArgumentException
        /// </summary>
        [Fact]
        public void DecryptString_NullKey_ThrowsArgumentException()
        {
            // Arrange
            string cipherText = "someEncryptedText";
            string key = null;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => AesEncryptionHelper.DecryptString(cipherText, key));
        }

        /// <summary>
        /// 测试解密方法的空密钥参数验证
        /// 验证：当密钥为空字符串时，应该抛出ArgumentException
        /// </summary>
        [Fact]
        public void DecryptString_EmptyKey_ThrowsArgumentException()
        {
            // Arrange
            string cipherText = "someEncryptedText";
            string key = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => AesEncryptionHelper.DecryptString(cipherText, key));
        }

        /// <summary>
        /// 测试解密方法的密钥长度验证
        /// 验证：当密钥长度小于16字符时，应该抛出ArgumentException
        /// </summary>
        [Fact]
        public void DecryptString_ShortKey_ThrowsArgumentException()
        {
            // Arrange
            string cipherText = "someEncryptedText";
            string key = "ShortKey";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => AesEncryptionHelper.DecryptString(cipherText, key));
        }

        /// <summary>
        /// 测试解密方法的无效密文处理
        /// 验证：当密文不是有效的十六进制字符串时，应该抛出FormatException
        /// </summary>
        [Fact]
        public void DecryptString_InvalidCipherText_ThrowsFormatException()
        {
            // Arrange
            string invalidCipherText = "invalidHexString";
            string key = "ThisIsASecretKey123456789012";

            // Act & Assert
            Assert.Throws<FormatException>(() => AesEncryptionHelper.DecryptString(invalidCipherText, key));
        }

        /// <summary>
        /// 测试解密方法的错误密钥处理
        /// 验证：当使用错误的密钥解密时，应该抛出CryptographicException
        /// </summary>
        [Fact]
        public void DecryptString_WrongKey_ThrowsException()
        {
            // Arrange
            string plainText = "Hello, World!";
            string correctKey = "ThisIsASecretKey123456789012";
            string wrongKey = "ThisIsAWrongSecretKey1234567890";
            
            string encryptedText = AesEncryptionHelper.EncryptString(plainText, correctKey);

            // Act & Assert
            Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(() => 
                AesEncryptionHelper.DecryptString(encryptedText, wrongKey));
        }

        /// <summary>
        /// 测试加密方法的随机IV生成
        /// 验证：对于相同的明文和密钥，每次加密应该产生不同的结果（因为IV是随机生成的）
        /// 但是解密后都应该得到相同的原始文本
        /// </summary>
        [Fact]
        public void EncryptString_SameInput_DifferentOutput_BecauseOfRandomIV()
        {
            // Arrange
            string plainText = "Hello, World!";
            string key = "ThisIsASecretKey123456789012";

            // Act
            string encryptedText1 = AesEncryptionHelper.EncryptString(plainText, key);
            string encryptedText2 = AesEncryptionHelper.EncryptString(plainText, key);

            // Assert
            Assert.NotEqual(encryptedText1, encryptedText2);
            
            // But both should decrypt to the same original text
            string decryptedText1 = AesEncryptionHelper.DecryptString(encryptedText1, key);
            string decryptedText2 = AesEncryptionHelper.DecryptString(encryptedText2, key);
            Assert.Equal(plainText, decryptedText1);
            Assert.Equal(plainText, decryptedText2);
        }

        /// <summary>
        /// 测试不同密钥的加密结果
        /// 验证：对于相同的明文，使用不同的密钥应该产生不同的加密结果
        /// </summary>
        [Fact]
        public void EncryptString_DifferentKeys_DifferentOutputs()
        {
            // Arrange
            string plainText = "Hello, World!";
            string key1 = "ThisIsASecretKey123456789012";
            string key2 = "ThisIsAnotherSecretKey1234567890";

            // Act
            string encryptedText1 = AesEncryptionHelper.EncryptString(plainText, key1);
            string encryptedText2 = AesEncryptionHelper.EncryptString(plainText, key2);

            // Assert
            Assert.NotEqual(encryptedText1, encryptedText2);
        }
    }
}