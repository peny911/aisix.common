using Aisix.Common;
using Aisix.Common.Model.Api;
using Aisix.Common.WebApi.Sample.Models;
using Aisix.Common.WebApi.Sample.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aisix.Common.WebApi.Sample.Controllers
{
    /// <summary>
    /// 测试控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ITestService _testService;
        private readonly ILogger<TestController> _logger;

        public TestController(ITestService testService, ILogger<TestController> logger)
        {
            _testService = testService;
            _logger = logger;
        }

        /// <summary>
        /// 根据ID获取测试实体
        /// </summary>
        /// <param name="id">主键ID</param>
        /// <returns>测试实体</returns>
        [HttpGet("{id}")]
        public async Task<ApiResult<TestEntity>> GetById(int id)
        {
            try
            {
                var result = await _testService.GetByIdAsync(id);
                return result == null
                    ? ApiResult<TestEntity>.Fail("未找到指定ID的测试实体")
                    : ApiResult<TestEntity>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取测试实体失败，ID: {Id}", id);
                return ApiResult<TestEntity>.Fail("获取测试实体失败");
            }
        }

        /// <summary>
        /// 获取所有测试实体
        /// </summary>
        /// <returns>测试实体列表</returns>
        [HttpGet]
        public async Task<ApiResult<List<TestEntity>>> GetAll()
        {
            try
            {
                var result = await _testService.GetAllAsync();
                return ApiResult<List<TestEntity>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有测试实体失败");
                return ApiResult<List<TestEntity>>.Fail("获取测试实体列表失败");
            }
        }

        /// <summary>
        /// 根据条件查询测试实体
        /// </summary>
        /// <param name="name">名称（模糊匹配）</param>
        /// <param name="isEnabled">是否启用</param>
        /// <returns>测试实体列表</returns>
        [HttpGet("search")]
        public async Task<ApiResult<List<TestEntity>>> Search([FromQuery] string? name = null, [FromQuery] bool? isEnabled = null)
        {
            try
            {
                var result = await _testService.GetByConditionAsync(name, isEnabled);
                return ApiResult<List<TestEntity>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询测试实体失败，Name: {Name}, IsEnabled: {IsEnabled}", name, isEnabled);
                return ApiResult<List<TestEntity>>.Fail("查询测试实体失败");
            }
        }

        /// <summary>
        /// 创建测试实体
        /// </summary>
        /// <param name="request">创建请求</param>
        /// <returns>创建的测试实体</returns>
        [HttpPost]
        public async Task<ApiResult<TestEntity>> Create([FromBody] CreateTestRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ApiResult<TestEntity>.Fail("请求数据验证失败");
            }

            try
            {
                var result = await _testService.CreateAsync(request);
                return ApiResult<TestEntity>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建测试实体失败，Name: {Name}", request.Name);
                return ApiResult<TestEntity>.Fail("创建测试实体失败");
            }
        }

        /// <summary>
        /// 更新测试实体
        /// </summary>
        /// <param name="id">主键ID</param>
        /// <param name="request">更新请求</param>
        /// <returns>更新后的测试实体</returns>
        [HttpPut("{id}")]
        public async Task<ApiResult<TestEntity>> Update(int id, [FromBody] UpdateTestRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ApiResult<TestEntity>.Fail("请求数据验证失败");
            }

            try
            {
                var result = await _testService.UpdateAsync(id, request);
                return result == null
                    ? ApiResult<TestEntity>.Fail("未找到指定ID的测试实体")
                    : ApiResult<TestEntity>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新测试实体失败，ID: {Id}", id);
                return ApiResult<TestEntity>.Fail("更新测试实体失败");
            }
        }

        /// <summary>
        /// 删除测试实体
        /// </summary>
        /// <param name="id">主键ID</param>
        /// <returns>删除结果</returns>
        [HttpDelete("{id}")]
        public async Task<ApiResult> Delete(int id)
        {
            try
            {
                var result = await _testService.DeleteAsync(id);
                return result
                    ? ApiResult.Success("删除成功")
                    : ApiResult.Fail("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除测试实体失败，ID: {Id}", id);
                return ApiResult.Fail("删除测试实体失败");
            }
        }

        /// <summary>
        /// 批量删除测试实体
        /// </summary>
        /// <param name="ids">主键ID列表</param>
        /// <returns>删除结果</returns>
        [HttpDelete("batch")]
        public async Task<ApiResult> DeleteBatch([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return ApiResult.Fail("请提供要删除的ID列表");
            }

            try
            {
                var result = await _testService.DeleteBatchAsync(ids);
                return result
                    ? ApiResult.Success($"成功删除 {ids.Count} 条记录")
                    : ApiResult.Fail("批量删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除测试实体失败，IDs: {Ids}", string.Join(",", ids));
                return ApiResult.Fail("批量删除测试实体失败");
            }
        }
    }
}