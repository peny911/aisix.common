using Newtonsoft.Json;

namespace Aisix.Common.Model.Api
{
    /// <summary>
    /// 分页组件实体类
    /// </summary>
    /// <typeparam name="T">泛型实体</typeparam>

    [Serializable]
    public class PagedInfo<T>
    {
        public PageItemVo Pager { get; set; }

        public List<T> Data { get; set; }

        public PagedInfo() {
            this.Pager = new PageItemVo();
        }
    }

    public class PageItemVo
    {
        /// <summary>
        /// 分页索引
        /// </summary>
        [JsonProperty("page_num")]
        public int PageNum { get; set; }
        /// <summary>
        /// 分页大小
        /// </summary>
        [JsonProperty("page_size")]
        public int PageSize { get; set; }
        /// <summary>
        /// 总记录数
        /// </summary>
        [JsonProperty("total")]
        public int TotalNumber { get; set; }
        /// <summary>
        /// 总页数
        /// </summary>
        [JsonProperty("total_page")]
        public int totalPage { get; set; }

        /// <summary>
        /// 是否有上一页
        /// </summary>
        [JsonProperty("has_previous_page")]
        public bool HasPreviousPage
        {
            get { return PageNum > 1; }
        }
        /// <summary>
        /// 是否有下一页
        /// </summary>
        [JsonProperty("has_next_page")]
        public bool HasNextPage
        {
            get { return PageNum + 1 < totalPage; }
        }
        //public object TotalField { get; set; }
    }

}