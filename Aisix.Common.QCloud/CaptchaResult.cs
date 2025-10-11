using TencentCloud.Captcha.V20190722;
using TencentCloud.Captcha.V20190722.Models;
using TencentCloud.Common;
using TencentCloud.Common.Profile;

namespace Aisix.Common.QCloud
{
    public class CaptchaResult
    {
        private readonly TencentCloudOptions _options;

        public CaptchaResult(TencentCloudOptions options)
        {
            _options = options;
        }

        public DescribeCaptchaResultResponse Check(string ticket, string randStr) 
        {
            // 检查ticket是否为空
            if (string.IsNullOrEmpty(ticket))
            {
                throw new ArgumentException("Ticket cannot be null or empty.", nameof(ticket));
            }

            try
            {
                Credential cred = new Credential
                {
                    SecretId = _options.SecretId,
                    SecretKey = _options.SecretKey,
                };

                // 实例化一个client选项，可选的，没有特殊需求可以跳过
                ClientProfile clientProfile = new ClientProfile();
                // 实例化一个http选项，可选的，没有特殊需求可以跳过
                HttpProfile httpProfile = new HttpProfile();
                httpProfile.Endpoint = ("captcha.tencentcloudapi.com");
                clientProfile.HttpProfile = httpProfile;

                // 实例化要请求产品的client对象,clientProfile是可选的
                CaptchaClient client = new CaptchaClient(cred, "", clientProfile);
                // 实例化一个请求对象,每个接口都会对应一个request对象
                DescribeCaptchaResultRequest req = new DescribeCaptchaResultRequest();
                req.Ticket = ticket;
                req.Randstr = randStr;
                req.CaptchaAppId = _options.captcha.CaptchaAppId;
                req.AppSecretKey = _options.captcha.AppSecretKey;
                req.CaptchaType = 9;
                req.UserIp = "127.0.0.1";
                // 返回的resp是一个DescribeCaptchaResultResponse的实例，与请求对象对应
                DescribeCaptchaResultResponse resp = client.DescribeCaptchaResultSync(req);

                return resp;
                // 输出json格式的字符串回包
                // Console.WriteLine(AbstractModel.ToJsonString(resp));
            }
            catch(Exception ex)
            {
                // 处理异常
                Console.WriteLine($"Error: {ex.Message}");
                throw;
            }
        }
    }
}
