using Microsoft.AspNetCore.Http;
using SqlSugar;
using System.Security.Claims;
using Aisix.Common;
using System.Collections.Generic;
using System.Linq;

namespace Aisix.Common.Db.QueryFilter
{
    /// <summary>
    /// 数据权限辅助类 - 提供数据权限过滤条件
    /// </summary>
    public class DataPermissionHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISqlSugarClient _sqlSugarClient;

        public DataPermissionHelper(IHttpContextAccessor httpContextAccessor, ISqlSugarClient sqlSugarClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _sqlSugarClient = sqlSugarClient;
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        public long GetCurrentUserId()
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            if (user != null && user.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
                {
                    return userId;
                }
            }
            return 0;
        }

        /// <summary>
        /// 获取当前用户部门ID
        /// </summary>
        public long? GetCurrentUserDeptId()
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            if (user != null && user.Identity?.IsAuthenticated == true)
            {
                var deptIdClaim = user.FindFirst("DeptId");
                if (deptIdClaim != null && long.TryParse(deptIdClaim.Value, out long deptId))
                {
                    return deptId;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取当前用户的最小数据权限范围
        /// </summary>
        public int GetCurrentUserDataScope()
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            if (user != null && user.Identity?.IsAuthenticated == true)
            {
                var dataScopes = user.FindAll("DataScope")
                    .Select(c => c.Value.Split(':'))
                    .Where(parts => parts.Length == 2 && int.TryParse(parts[1], out _))
                    .Select(parts => int.Parse(parts[1]))
                    .ToList();

                if (dataScopes.Any())
                {
                    // 返回最小值（全部数据=1最宽松，仅本人=5最严格）
                    return dataScopes.Min();
                }
            }

            // 默认返回全部数据权限
            return 1;
        }

        /// <summary>
        /// 根据数据权限范围获取部门ID列表
        /// </summary>
        /// <param name="dataScope">数据权限范围</param>
        /// <param name="customDeptIds">自定义部门ID列表（当dataScope=2时使用）</param>
        /// <returns>允许访问的部门ID列表</returns>
        public List<long> GetDeptIdsByDataScope(int dataScope, List<long>? customDeptIds = null)
        {
            var deptIds = new List<long>();
            var currentDeptId = GetCurrentUserDeptId();

            switch (dataScope)
            {
                case 1: // 全部数据权限
                    // 返回空列表表示不限制
                    break;

                case 2: // 自定义数据权限
                    if (customDeptIds != null && customDeptIds.Any())
                    {
                        deptIds.AddRange(customDeptIds);
                    }
                    break;

                case 3: // 本部门数据权限
                    if (currentDeptId.HasValue)
                    {
                        deptIds.Add(currentDeptId.Value);
                    }
                    break;

                case 4: // 本部门及以下数据权限
                    if (currentDeptId.HasValue)
                    {
                        deptIds.AddRange(GetDeptAndChildrenIds(currentDeptId.Value));
                    }
                    break;

                case 5: // 仅本人数据权限
                    // 不返回部门ID，调用方应该使用用户ID过滤
                    break;
            }

            return deptIds;
        }

        /// <summary>
        /// 获取部门及其所有子部门的ID列表
        /// </summary>
        private List<long> GetDeptAndChildrenIds(long deptId)
        {
            var result = new List<long> { deptId };

            try
            {
                // 递归查询子部门
                var db = _sqlSugarClient.AsTenant().GetConnection("default");
                var children = db.Queryable<dynamic>()
                    .AS("sys_dept")
                    .Where("parent_id = @deptId and delete_flag = 0", new { deptId })
                    .Select<long>("id")
                    .ToList();

                foreach (var childId in children)
                {
                    result.AddRange(GetDeptAndChildrenIds(childId));
                }
            }
            catch
            {
                // 如果查询失败，只返回当前部门ID
            }

            return result;
        }

        /// <summary>
        /// 为用户查询应用数据权限条件
        /// </summary>
        /// <param name="query">原始查询</param>
        /// <param name="customDeptIds">自定义部门ID列表（仅当dataScope=2时使用）</param>
        /// <returns>应用数据权限后的查询</returns>
        public ISugarQueryable<T> ApplyDataPermissionForUser<T>(ISugarQueryable<T> query, List<long>? customDeptIds = null) where T : class
        {
            var dataScope = GetCurrentUserDataScope();
            var userId = GetCurrentUserId();
            var deptId = GetCurrentUserDeptId();

            // 全部数据权限，不过滤
            if (dataScope == 1)
            {
                return query;
            }

            // 仅本人数据权限
            if (dataScope == 5)
            {
                return query.Where($"id = {userId}");
            }

            // 部门数据权限（本部门、本部门及以下、自定义）
            var deptIds = GetDeptIdsByDataScope(dataScope, customDeptIds);
            if (deptIds.Any())
            {
                return query.Where($"dept_id in ({string.Join(",", deptIds)})");
            }

            // 如果没有有效的部门ID，返回空结果
            return query.Where("1 = 0");
        }
    }
}
