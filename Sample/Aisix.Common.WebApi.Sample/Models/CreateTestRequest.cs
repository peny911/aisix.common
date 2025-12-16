using System.ComponentModel.DataAnnotations;

namespace Aisix.Common.WebApi.Sample.Models
{
    /// <summary>
    /// 创建测试实体请求
    /// </summary>
    public class CreateTestRequest
    {
        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// 数值
        /// </summary>
        public decimal? Value { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }
}