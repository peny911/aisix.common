using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace Aisix.Common.WebApi.Sample.Models
{
    /// <summary>
    /// 测试实体类
    /// </summary>
    [SugarTable("test")]
    public class TestEntity
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        [StringLength(100)]
        [SugarColumn(Length = 100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        [StringLength(500)]
        [SugarColumn(Length = 500)]
        public string? Description { get; set; }

        /// <summary>
        /// 数值
        /// </summary>
        public decimal? Value { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        [SugarColumn(IsNullable = false)]
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? UpdatedTime { get; set; }
    }
}