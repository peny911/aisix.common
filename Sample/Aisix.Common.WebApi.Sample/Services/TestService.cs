using Aisix.Common.Db.Service;
using Aisix.Common.WebApi.Sample.Models;
using SqlSugar;

namespace Aisix.Common.WebApi.Sample.Services
{
    /// <summary>
    /// 测试服务实现
    /// </summary>
    public class TestService : BaseService<TestEntity>, ITestService
    {
        private readonly ILogger<TestService> _logger;

        public TestService(ILogger<TestService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 根据ID获取测试实体
        /// </summary>
        public async Task<TestEntity?> GetByIdAsync(int id)
        {
            try
            {
                return await base.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取测试实体失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 获取所有测试实体
        /// </summary>
        public async Task<List<TestEntity>> GetAllAsync()
        {
            try
            {
                return await base.GetListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有测试实体失败");
                throw;
            }
        }

        /// <summary>
        /// 根据条件查询测试实体
        /// </summary>
        public async Task<List<TestEntity>> GetByConditionAsync(string? name = null, bool? isEnabled = null)
        {
            try
            {
                var expression = Expressionable.Create<TestEntity>()
                    .AndIF(!string.IsNullOrWhiteSpace(name), t => t.Name.Contains(name!))
                    .AndIF(isEnabled.HasValue, t => t.IsEnabled == isEnabled!.Value)
                    .ToExpression();

                return await base.GetListAsync(expression);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据条件查询测试实体失败，Name: {Name}, IsEnabled: {IsEnabled}", name, isEnabled);
                throw;
            }
        }

        /// <summary>
        /// 创建测试实体
        /// </summary>
        public async Task<TestEntity> CreateAsync(CreateTestRequest request)
        {
            try
            {
                var entity = new TestEntity
                {
                    Name = request.Name,
                    Description = request.Description,
                    Value = request.Value,
                    IsEnabled = request.IsEnabled,
                    CreatedTime = DateTime.Now
                };

                await base.InsertAsync(entity);
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建测试实体失败，Name: {Name}", request.Name);
                throw;
            }
        }

        /// <summary>
        /// 更新测试实体
        /// </summary>
        public async Task<TestEntity?> UpdateAsync(int id, UpdateTestRequest request)
        {
            try
            {
                var entity = await base.GetByIdAsync(id);
                if (entity == null)
                    return null;

                entity.Name = request.Name;
                entity.Description = request.Description;
                entity.Value = request.Value;
                entity.IsEnabled = request.IsEnabled;
                entity.UpdatedTime = DateTime.Now;

                await base.UpdateAsync(entity);
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新测试实体失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 删除测试实体
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var result = await base.DeleteByIdAsync(id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除测试实体失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 批量删除测试实体
        /// </summary>
        public async Task<bool> DeleteBatchAsync(List<int> ids)
        {
            try
            {
                if (ids == null || ids.Count == 0)
                    return true;

                var result = await base.DeleteByIdsAsync(ids.Select(id => (object)id).ToArray());
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除测试实体失败，IDs: {Ids}", string.Join(",", ids));
                throw;
            }
        }
    }
}