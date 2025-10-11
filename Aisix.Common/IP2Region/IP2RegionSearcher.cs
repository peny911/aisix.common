using IP2Region.Net.Abstractions;
using IP2Region.Net.XDB;
using System.Net;

namespace Aisix.Common.IP2Region
{
    internal class IP2RegionSearcher : IIP2RegionSearcher
    {
        private readonly ISearcher _internalSearcher;

        public IP2RegionSearcher(string xdbFilePath, CachePolicy cachePolicy = CachePolicy.Content)
        {
            _internalSearcher = new Searcher(cachePolicy, xdbFilePath);
        }

        public string Search(string ip) => _internalSearcher.Search(ip) ?? string.Empty;
        public string Search(IPAddress ip) => _internalSearcher.Search(ip) ?? string.Empty;
        public string Search(uint ip) => _internalSearcher.Search(ip) ?? string.Empty;
        public GeoLocationInfo GetRegion(string ip)
        {
            var regionStr = this.Search(ip) ?? string.Empty;

            // regionStr由“国家|区域|省份|城市|区/县|ISP”组成，将其拆分为Region对象
            // 兼容regionStr为空或分割后不足6项的情况
            var regionParts = regionStr.Split('|', StringSplitOptions.None);
            // 保证数组长度为6
            Array.Resize(ref regionParts, 6);

            return new GeoLocationInfo
            {
                Country = regionParts[0] ?? string.Empty,
                Geo = regionParts[1] ?? string.Empty,
                Province = regionParts[2] ?? string.Empty,
                City = regionParts[3] ?? string.Empty,
                District = regionParts[4] ?? string.Empty,
                Isp = regionParts[5] ?? string.Empty
            };
        }
    }
}
