namespace Aisix.Common.IP2Region
{
    public class GeoLocationInfo
    {
        public string Country { get; set; }
        public string Geo { get; internal set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string Isp { get; set; }
    }
}
