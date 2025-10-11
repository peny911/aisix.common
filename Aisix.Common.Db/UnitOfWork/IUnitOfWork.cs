using SqlSugar;

namespace Aisix.Common.Db.UnitOfWork
{
    /// <summary>
    /// SqlSugar工作单元接口（尚未完成）
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// 开始事务
        /// </summary>
        Task BeginTransactionAsync();

        /// <summary>
        /// 提交事务
        /// </summary>
        Task CommitAsync();

        /// <summary>
        /// 回滚事务
        /// </summary>
        Task RollbackAsync();

        /// <summary>
        /// 获取SqlSugar的ITenant对象（多租户支持）
        /// </summary>
        ITenant GetTenant();
    }
}
