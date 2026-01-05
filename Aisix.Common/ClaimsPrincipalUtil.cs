using System.Security.Claims;

namespace Aisix.Common
{
    public static class ClaimsPrincipalUtil
    {
        public static long GetUserId(this ClaimsPrincipal user)
        {
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
        /// 获取用户部门ID
        /// </summary>
        public static long? GetDeptId(this ClaimsPrincipal user)
        {
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
        /// 获取用户的最小数据权限范围（多个角色取最宽松的权限）
        /// </summary>
        public static int GetMinDataScope(this ClaimsPrincipal user)
        {
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
        /// 获取用户所有角色的数据权限映射
        /// </summary>
        public static Dictionary<string, int> GetRoleDataScopes(this ClaimsPrincipal user)
        {
            var result = new Dictionary<string, int>();
            if (user != null && user.Identity?.IsAuthenticated == true)
            {
                var dataScopes = user.FindAll("DataScope");
                foreach (var claim in dataScopes)
                {
                    var parts = claim.Value.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[1], out var scope))
                    {
                        result[parts[0]] = scope;
                    }
                }
            }
            return result;
        }
    }
}
