using COSSTS;
using Newtonsoft.Json;

namespace Aisix.Common.QCloud
{
    public class TencentCloudService
    {
        public static CredentialsResuslt GetCredential(string bucket, string region, string allowPrefix, string[] allowActions, string secretId, string secretKey)
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values.Add("bucket", bucket);
            values.Add("region", region);
            values.Add("allowPrefix", allowPrefix);
            values.Add("allowActions", allowActions);
            values.Add("durationSeconds", 1800);
            values.Add("secretId", secretId);
            values.Add("secretKey", secretKey);

            Dictionary<string, object> credential = STSClient.genCredential(values);

            var result = new CredentialsResuslt()
            {
                ExpiredTime = Convert.ToInt32(credential["ExpiredTime"]),
                Credentials = JsonConvert.DeserializeObject<CredentialsResuslt.CredentialsItem>(credential["Credentials"].ToString()),
                Expiration = Convert.ToDateTime(credential["Expiration"]).AddHours(8),
                StartTime = Convert.ToInt32(credential["StartTime"]),
                RequestId = credential["RequestId"].ToString()
            };

            return result;
        }
    }
}
