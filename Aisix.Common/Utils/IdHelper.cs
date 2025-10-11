using System.Text;

namespace Aisix.Common.Utils
{
    public class IdHelper
    {
        public static string GenerateGUID()
        {
            return Guid.NewGuid().ToString();
        }

        public static string GenerateCUID()
        {
            return CuidGenerator.Generate();
        }

        public static string GenerateRandomID(int length)
        {
            var random = new Random();
            var result = new StringBuilder(length);

            // 由于Random.Next(int minValue, int maxValue)不会生成maxValue，
            // 因此为了确保数字9也被包括进去，我们将上限设置为10。
            for (int i = 0; i < length; i++)
            {
                result.Append(random.Next(0, 10));
            }

            return result.ToString();
        }
    }

}

