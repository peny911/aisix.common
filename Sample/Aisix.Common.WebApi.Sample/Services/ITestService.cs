using Aisix.Common.WebApi.Sample.Models;

namespace Aisix.Common.WebApi.Sample.Services
{
    /// <summary>
    /// 测试服务接口
    /// </summary>
    public interface ITestService
    {
        /// <summary>
        /// 根据ID获取测试实体
        /// </summary>
        Task<TestEntity?> GetByIdAsync(int id);

        /// <summary>
        /// 获取所有测试实体
        /// </summary>
        Task<List<TestEntity>> GetAllAsync();

        /// <summary>
        /// 根据条件查询测试实体
        /// </summary>
        Task<List<TestEntity>> GetByConditionAsync(string? name = null, bool? isEnabled = null);

        /// <summary>
        /// 创建测试实体
        /// </summary>
        Task<TestEntity> CreateAsync(CreateTestRequest request);

        /// <summary>
        /// 更新测试实体
        /// </summary>
        Task<TestEntity?> UpdateAsync(int id, UpdateTestRequest request);

        /// <summary>
        /// 删除测试实体
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// 批量删除测试实体
        /// </summary>
        Task<bool> DeleteBatchAsync(List<int> ids);
    }
}