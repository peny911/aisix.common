using System.Net;

namespace Aisix.Common.IP2Region
{
    /// <summary>
    /// xdb 支持亿级别的 IP 数据段行数，默认的 region 信息都固定了格式：国家|区域|省份|城市|ISP，缺省的地域信息默认是0。
    /// </summary>
    public interface IIP2RegionSearcher : ISingletonDependency
    {
        string? Search(string ipStr);
        string? Search(IPAddress ipAddress);
        string? Search(uint ipAddress);
        GeoLocationInfo GetRegion(string ip);
    }
}
