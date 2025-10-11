/*
* ==============================================================================
*
* FileName: PageParm.cs
* Created: 2020/5/31 21:34:53
* Author: Patrick
* Description: 
*
* ==============================================================================
*/
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Aisix.Common.Model.Api
{
    public class PageParm
    {
        /// <summary>
        /// 当前页
        /// </summary>
        [JsonProperty("page_num")]
        public int? page_num { get; set; } = 1;

        /// <summary>
        /// 每页总条数
        /// </summary>
        public int? page_size { get; set; } = 20;

        /// <summary>
        /// 排序字段
        /// </summary>
        public string? order_by { get; set; } = "id";

        /// <summary>
        /// 排序方式
        /// </summary>
        public string? sort { get; set; } = "desc";

    }
}
