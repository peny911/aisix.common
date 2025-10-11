using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Aisix.Common.Model.Api
{
    public record ApiResult
    {
        // [JsonPropertyName("code")]
        [JsonProperty("code")] public int Code { get; set; }

        [JsonProperty("msg")] public string? Msg { get; set; }

        [JsonProperty("data")] public object? Data { get; set; }

        [JsonProperty("timestamp")] public string Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();

        public static ApiResult Success(object? body = null)
        {
            return new ApiResult
            {
                Code = 200,
                Data = body,
                Msg = "success"
            };
        }

        public static ApiResult Fail(string? errorMessage, int code = 400)
        {
            return new ApiResult
            {
                Code = code,
                Msg = errorMessage != null ? errorMessage : "error"
            };
        }
    }

    public class ApiResult<T> where T : class
    {
        // [JsonPropertyName("code")]
        [JsonProperty("code")] public int Code { get; set; }

        [JsonProperty("msg")] public string? Msg { get; set; }

        [JsonProperty("data")] public T? Data { get; set; }

        [JsonProperty("timestamp")] public string Timestamp { get; set; } = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();

        public static ApiResult<T> Success(T obj)
        {
            return new ApiResult<T>
            {
                Code = 200,
                Data = obj,
                Msg = "success"
            };
        }

        public static ApiResult<T> Fail(string? errorMessage, int code = 400)
        {
            return new ApiResult<T>
            {
                Code = code,
                Msg = errorMessage != null ? errorMessage : "error"
            };
        }
    }
}
