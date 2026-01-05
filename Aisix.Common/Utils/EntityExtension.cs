using System.Collections.Concurrent;
using System.Reflection;

namespace Aisix.Common.Utils
{
    /// <summary>
    /// 实体扩展方法,用于自动设置创建、更新和删除时的审计字段
    /// </summary>
    public static class EntityExtension
    {
        private const string CreatedKey = "created";
        private const string LastModifiedKey = "last_modified";
        private const string CreatedIdKey = "created_id";
        private const string LastModifiedIdKey = "last_modified_id";
        private const string DeleteTimeKey = "delete_time";

        // 使用线程安全的字典缓存类型的属性信息,提升反射性能
        private static readonly ConcurrentDictionary<Type, PropertyCache> PropertyCaches = new();

        /// <summary>
        /// 属性缓存类,避免重复反射
        /// </summary>
        private class PropertyCache
        {
            public PropertyInfo? Created { get; init; }
            public PropertyInfo? LastModified { get; init; }
            public PropertyInfo? CreatedId { get; init; }
            public PropertyInfo? LastModifiedId { get; init; }
            public PropertyInfo? DeleteTime { get; init; }
        }

        /// <summary>
        /// 获取或创建类型的属性缓存
        /// </summary>
        private static PropertyCache GetPropertyCache(Type type)
        {
            return PropertyCaches.GetOrAdd(type, t => new PropertyCache
            {
                Created = t.GetProperty(CreatedKey),
                LastModified = t.GetProperty(LastModifiedKey),
                CreatedId = t.GetProperty(CreatedIdKey),
                LastModifiedId = t.GetProperty(LastModifiedIdKey),
                DeleteTime = t.GetProperty(DeleteTimeKey)
            });
        }

        #region 单实体扩展方法

        /// <summary>
        /// 设置实体的创建审计字段(created, last_modified, created_id, last_modified_id)
        /// </summary>
        /// <typeparam name="TSource">实体类型</typeparam>
        /// <param name="source">实体对象</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <returns>设置后的实体</returns>
        public static TSource ToCreate<TSource>(this TSource source, long currentUserId) where TSource : class
        {
            if (source == null)
            {
                return source;
            }

            var cache = GetPropertyCache(typeof(TSource));
            var now = DateTime.Now;

            cache.Created?.SetValue(source, now, null);
            cache.LastModified?.SetValue(source, now, null);
            cache.CreatedId?.SetValue(source, currentUserId, null);
            cache.LastModifiedId?.SetValue(source, currentUserId, null);

            return source;
        }

        /// <summary>
        /// 设置实体的更新审计字段(last_modified, last_modified_id)
        /// </summary>
        /// <typeparam name="TSource">实体类型</typeparam>
        /// <param name="source">实体对象</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <returns>设置后的实体</returns>
        public static TSource ToUpdate<TSource>(this TSource source, long currentUserId) where TSource : class
        {
            if (source == null)
            {
                return source;
            }

            var cache = GetPropertyCache(typeof(TSource));
            var now = DateTime.Now;

            cache.LastModified?.SetValue(source, now, null);
            cache.LastModifiedId?.SetValue(source, currentUserId, null);

            return source;
        }

        /// <summary>
        /// 设置实体的删除审计字段(delete_time, last_modified, last_modified_id)
        /// </summary>
        /// <typeparam name="TSource">实体类型</typeparam>
        /// <param name="source">实体对象</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <returns>设置后的实体</returns>
        public static TSource ToDelete<TSource>(this TSource source, long currentUserId) where TSource : class
        {
            if (source == null)
            {
                return source;
            }

            var cache = GetPropertyCache(typeof(TSource));
            var now = DateTime.Now;

            cache.DeleteTime?.SetValue(source, now, null);
            cache.LastModified?.SetValue(source, now, null);
            cache.LastModifiedId?.SetValue(source, currentUserId, null);

            return source;
        }

        #endregion

        #region 集合扩展方法

        /// <summary>
        /// 批量设置实体集合的创建审计字段
        /// </summary>
        /// <typeparam name="TSource">实体类型</typeparam>
        /// <param name="sources">实体集合</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <returns>设置后的实体集合</returns>
        public static IEnumerable<TSource> ToCreate<TSource>(this IEnumerable<TSource> sources, long currentUserId) where TSource : class
        {
            if (sources == null)
            {
                return sources;
            }

            var cache = GetPropertyCache(typeof(TSource));
            var now = DateTime.Now;

            foreach (var source in sources)
            {
                if (source != null)
                {
                    cache.Created?.SetValue(source, now, null);
                    cache.LastModified?.SetValue(source, now, null);
                    cache.CreatedId?.SetValue(source, currentUserId, null);
                    cache.LastModifiedId?.SetValue(source, currentUserId, null);
                }
            }

            return sources;
        }

        /// <summary>
        /// 批量设置实体集合的更新审计字段
        /// </summary>
        /// <typeparam name="TSource">实体类型</typeparam>
        /// <param name="sources">实体集合</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <returns>设置后的实体集合</returns>
        public static IEnumerable<TSource> ToUpdate<TSource>(this IEnumerable<TSource> sources, long currentUserId) where TSource : class
        {
            if (sources == null)
            {
                return sources;
            }

            var cache = GetPropertyCache(typeof(TSource));
            var now = DateTime.Now;

            foreach (var source in sources)
            {
                if (source != null)
                {
                    cache.LastModified?.SetValue(source, now, null);
                    cache.LastModifiedId?.SetValue(source, currentUserId, null);
                }
            }

            return sources;
        }

        /// <summary>
        /// 批量设置实体集合的删除审计字段
        /// </summary>
        /// <typeparam name="TSource">实体类型</typeparam>
        /// <param name="sources">实体集合</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <returns>设置后的实体集合</returns>
        public static IEnumerable<TSource> ToDelete<TSource>(this IEnumerable<TSource> sources, long currentUserId) where TSource : class
        {
            if (sources == null)
            {
                return sources;
            }

            var cache = GetPropertyCache(typeof(TSource));
            var now = DateTime.Now;

            foreach (var source in sources)
            {
                if (source != null)
                {
                    cache.DeleteTime?.SetValue(source, now, null);
                    cache.LastModified?.SetValue(source, now, null);
                    cache.LastModifiedId?.SetValue(source, currentUserId, null);
                }
            }

            return sources;
        }

        /// <summary>
        /// 批量设置实体列表的创建审计字段(针对 List 的便捷方法)
        /// </summary>
        /// <typeparam name="TSource">实体类型</typeparam>
        /// <param name="sources">实体列表</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <returns>设置后的实体列表</returns>
        public static List<TSource> ToCreate<TSource>(this List<TSource> sources, long currentUserId) where TSource : class
        {
            if (sources == null)
            {
                return sources;
            }

            var cache = GetPropertyCache(typeof(TSource));
            var now = DateTime.Now;

            foreach (var source in sources)
            {
                if (source != null)
                {
                    cache.Created?.SetValue(source, now, null);
                    cache.LastModified?.SetValue(source, now, null);
                    cache.CreatedId?.SetValue(source, currentUserId, null);
                    cache.LastModifiedId?.SetValue(source, currentUserId, null);
                }
            }

            return sources;
        }

        /// <summary>
        /// 批量设置实体列表的更新审计字段(针对 List 的便捷方法)
        /// </summary>
        /// <typeparam name="TSource">实体类型</typeparam>
        /// <param name="sources">实体列表</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <returns>设置后的实体列表</returns>
        public static List<TSource> ToUpdate<TSource>(this List<TSource> sources, long currentUserId) where TSource : class
        {
            if (sources == null)
            {
                return sources;
            }

            var cache = GetPropertyCache(typeof(TSource));
            var now = DateTime.Now;

            foreach (var source in sources)
            {
                if (source != null)
                {
                    cache.LastModified?.SetValue(source, now, null);
                    cache.LastModifiedId?.SetValue(source, currentUserId, null);
                }
            }

            return sources;
        }

        /// <summary>
        /// 批量设置实体列表的删除审计字段(针对 List 的便捷方法)
        /// </summary>
        /// <typeparam name="TSource">实体类型</typeparam>
        /// <param name="sources">实体列表</param>
        /// <param name="currentUserId">当前用户ID</param>
        /// <returns>设置后的实体列表</returns>
        public static List<TSource> ToDelete<TSource>(this List<TSource> sources, long currentUserId) where TSource : class
        {
            if (sources == null)
            {
                return sources;
            }

            var cache = GetPropertyCache(typeof(TSource));
            var now = DateTime.Now;

            foreach (var source in sources)
            {
                if (source != null)
                {
                    cache.DeleteTime?.SetValue(source, now, null);
                    cache.LastModified?.SetValue(source, now, null);
                    cache.LastModifiedId?.SetValue(source, currentUserId, null);
                }
            }

            return sources;
        }

        #endregion
    }
}
