using NLog;
using System.Net.Http.Json;

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
                var response = await client.PostAsJsonAsync<SendItem>("", new SendItem
                {
                    scope = wxe.scope.ToString(),
                    body = wxe.body,
                });

                if (response.IsSuccessStatusCode)
                {
                    return true;
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
