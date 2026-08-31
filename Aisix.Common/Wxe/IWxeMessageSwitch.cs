namespace Aisix.Common.Wxe
{
    public interface IWxeMessageSwitch
    {
        /// <summary>
        /// 判断企业微信通知是否允许发送；返回 false 时发送器会静默跳过外部请求。
        /// </summary>
        Task<bool> IsEnabledAsync();
    }

    public class AlwaysEnabledWxeMessageSwitch : IWxeMessageSwitch
    {
        public Task<bool> IsEnabledAsync()
        {
            return Task.FromResult(true);
        }
    }
}
