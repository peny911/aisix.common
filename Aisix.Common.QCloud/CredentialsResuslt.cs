namespace Aisix.Common.QCloud
{
    public class CredentialsResuslt
    {
        public CredentialsItem Credentials { get; set; }
        public int ExpiredTime { get; set; }
        public DateTime Expiration { get; set; }
        public string RequestId { get; set; }
        public int StartTime { get; set; }


        public class CredentialsItem
        {
            public string Token { get; set; }
            public string TmpSecretId { get; set; }
            public string TmpSecretKey { get; set; }
        }
    }
}
