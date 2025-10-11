// 文件：Utils/HttpClientHelper.cs
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Aisix.Common.Utils
{
    /// <summary>
    /// 通用 HTTP 客户端帮助接口
    /// </summary>
    public interface IHttpClientHelper
    {
        Task<T?> GetAsync<T>(string requestUri);
        Task<string> GetStringAsync(string requestUri);
        Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest content);
        Task<string> PostAsync<TRequest>(string requestUri, TRequest content);
        Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest content);
        Task DeleteAsync(string requestUri);
    }

    /// <summary>
    /// 基于 HttpClientFactory 的通用 HTTP 请求帮助类
    /// </summary>
    public class HttpClientHelper : IHttpClientHelper
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public HttpClientHelper(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<T?> GetAsync<T>(string requestUri)
        {
            try
            {
                using var resp = await _httpClient.GetAsync(requestUri);
                resp.EnsureSuccessStatusCode();
                var stream = await resp.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new HttpRequestException($"HTTP GET request failed for {requestUri}. Status: {ex.StatusCode}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException($"HTTP GET request timed out for {requestUri}", ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to deserialize JSON response from {requestUri}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error during HTTP GET request to {requestUri}", ex);
            }
        }

        public async Task<string> GetStringAsync(string requestUri)
        {
            try
            {
                using var resp = await _httpClient.GetAsync(requestUri);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new HttpRequestException($"HTTP GET request failed for {requestUri}. Status: {ex.StatusCode}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new TimeoutException($"HTTP GET request timed out for {requestUri}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unexpected error during HTTP GET request to {requestUri}", ex);
            }
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest content)
        {
            var json = JsonSerializer.Serialize(content, _jsonOptions);
            using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _httpClient.PostAsync(requestUri, httpContent);
            resp.EnsureSuccessStatusCode();
            var stream = await resp.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<TResponse>(stream, _jsonOptions);
        }

        public async Task<string> PostAsync<TRequest>(string requestUri, TRequest content)
        {
            var json = JsonSerializer.Serialize(content, _jsonOptions);
            using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _httpClient.PostAsync(requestUri, httpContent);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest content)
        {
            var json = JsonSerializer.Serialize(content, _jsonOptions);
            using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _httpClient.PutAsync(requestUri, httpContent);
            resp.EnsureSuccessStatusCode();
            var stream = await resp.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<TResponse>(stream, _jsonOptions);
        }

        public async Task DeleteAsync(string requestUri)
        {
            using var resp = await _httpClient.DeleteAsync(requestUri);
            resp.EnsureSuccessStatusCode();
        }
    }
}
