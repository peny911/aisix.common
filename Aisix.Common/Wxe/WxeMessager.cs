using Newtonsoft.Json;
using NLog;
using System.Text;

namespace Aisix.Common.Wxe
{
    public interface IWxeMessager
    {
        Task<bool> SendAsync(MessageItem wxe);
    }

    public class WxeMessager : IWxeMessager
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IHttpClientFactory _httpClientFactory;

        public WxeMessager(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> SendAsync(MessageItem wxe)
        {
            try
            {
#if DEBUG
                wxe.body = $"【测试】{wxe.body}";
#endif
                var client = _httpClientFactory.CreateClient("wxe");

                var sendItem = new SendItem
                {
                    scope = wxe.scope.ToString(),
                    body = wxe.body,
                };

                var json = JsonConvert.SerializeObject(sendItem);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    // 检查业务状态
                    if (responseBody.Contains("\"status\":1") || responseBody.Contains("\"status\": 1"))
                    {
                        return true;
                    }
                    else
                    {
                        var errMsg = $"Wxe message business failed. Response: {responseBody}";
                        Console.WriteLine(errMsg);
                        _logger.Warn(errMsg);
                    }
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var errMsg = $"Wxe message request failed. Status: {response.StatusCode}, Response: {responseBody}";
                    Console.WriteLine(errMsg);
                    _logger.Error(errMsg);
                }
            }
            catch (HttpRequestException ex)
            {
                var errMsg = $"HTTP request failed when sending wxe message. Status: {ex.StatusCode}, Message: {ex.Message}";
                Console.WriteLine(errMsg);
                _logger.Error(ex, errMsg);
            }
            catch (TaskCanceledException ex)
            {
                var errMsg = $"Wxe message request timed out. Message: {ex.Message}";
                Console.WriteLine(errMsg);
                _logger.Error(ex, errMsg);
            }
            catch (InvalidOperationException ex)
            {
                var errMsg = $"Invalid operation when sending wxe message. Message: {ex.Message}";
                Console.WriteLine(errMsg);
                _logger.Error(ex, errMsg);
            }
            catch (Exception ex)
            {
                var errMsg = $"Unexpected error when sending wxe message. {ex.Message}";
                Console.WriteLine(errMsg);
                _logger.Error(ex, errMsg);
            }

            return false;
        }

        private class SendItem
        {
            public string scope { get; set; }
            public string body { get; set; }
        }
    }
}
