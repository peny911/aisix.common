namespace Aisix.Common.IP2Region
{
    public class GeoLocationInfo
    {
        public string Country { get; set; } = string.Empty;
        public string Geo { get; internal set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Isp { get; set; } = string.Empty;
    }
}
