namespace Aisix.Common.Wxe
{
    public class MessageItem
    {
        public Scope scope { get; set; }
        public string body { get; set; }
    }

    public enum Scope
    {
        None,
        Notification,
        Report,
        Warning,
        HeartBeat,
    }
}
