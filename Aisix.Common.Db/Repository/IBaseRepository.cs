using Aisix.Common.Model.Api;
using SqlSugar;
using System.Linq.Expressions;

namespace Aisix.Common.Db.Repository
{
    public interface IBaseRepository<T> : ISimpleClient<T>, IScopedDependency /*ITransientDependency*/ where T : class, new()
    {
        #region 添加操作
        /// <summary>
        /// 添加一条数据
        /// </summary>
        /// <param name="parm"></param>
        /// <returns></returns>
        int Add(T parm);
        long AddReturnBigId(T parm);

        /// <summary>
        /// 批量添加数据
        /// </summary>
        /// <param name="parm">List<T></param>
        /// <returns></returns>
        int Add(List<T> parm);

        Task<int> AddAsync(T parm);
        Task<long> AddReturnBigIdAsync(T parm);

        Task<int> AddAsync(List<T> parm);

        Task<List<long>> AddSplitReturnSnowflakeIdListAsync(List<T> parm);

        #endregion

        #region 查询操作

        /// <summary>
        /// 根据条件查询数据是否存在
        /// </summary>
        /// <param name="where">条件表达式树</param>
        /// <returns></returns>
        bool Any(Expression<Func<T, bool>> where);
        Task<bool> AnyAsync(Expression<Func<T, bool>> where);

        /// <summary>
        /// 根据条件合计字段
        /// </summary>
        /// <param name="where">条件表达式树</param>
        /// <returns></returns>
        TResult Sum<TResult>(Expression<Func<T, bool>> where, Expression<Func<T, TResult>> field);
        Task<TResult> SumAsync<TResult>(Expression<Func<T, bool>> where, Expression<Func<T, TResult>> field);


        /// <summary>
        /// 根据主值查询单条数据
        /// </summary>
        /// <param name="pkValue">主键值</param>
        /// <returns>泛型实体</returns>
        T GetId(object pkValue);
        Task<T> GetIdAsync(object pkValue);


        /// <summary>
        /// 根据主键查询多条数据
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        List<T> GetIn(object[] ids);
        Task<List<T>> GetInAsync(object[] ids);


        /// <summary>
        /// 根据条件取条数
        /// </summary>
        /// <param name="where">条件表达式树</param>
        /// <returns></returns>
        int GetCount(Expression<Func<T, bool>> where);
        Task<int> GetCountAsync(Expression<Func<T, bool>> where);


        /// <summary>
        /// 查询所有数据(无分页,请慎用)
        /// </summary>
        /// <returns></returns>
        List<T> GetAll(bool useCache = false, int cacheSecond = 3600);
        Task<List<T>> GetAllAsync(bool useCache = false, int cacheSecond = 3600);


        /// <summary>
        /// 获得一条数据
        /// </summary>
        /// <param name="where">Expression<Func<T, bool>></param>
        /// <returns></returns>
        new T GetFirst(Expression<Func<T, bool>> where);
        new Task<T> GetFirstAsync(Expression<Func<T, bool>> where);


        /// <summary>
        /// 获得一条数据
        /// </summary>
        /// <param name="parm">string</param>
        /// <returns></returns>
        T GetFirst(string parm);
        Task<T> GetFirstAsync(string parm);

        /// <summary>
        /// 根据条件查询分页数据
        /// </summary>
        /// <param name="where"></param>
        /// <param name="parm"></param>
        /// <returns></returns>
        PagedInfo<T> GetPages(Expression<Func<T, bool>> where, PageParm parm);
        Task<PagedInfo<T>> GetPagesAsync(Expression<Func<T, bool>> where, PageParm parm);

        /// <summary>
        /// 根据条件查询数据
        /// </summary>
        /// <param name="where">条件表达式树</param>
        /// <returns></returns>
        List<T> GetWhere(Expression<Func<T, bool>> where, bool useCache = false, int cacheSecond = 3600);
        Task<List<T>> GetWhereAsync(Expression<Func<T, bool>> where, bool useCache = false, int cacheSecond = 3600);

        /// <summary>
        /// 根据条件查询数据
        /// </summary>
        /// <param name="where">条件表达式树</param>
        /// <returns></returns>
        Task<List<T>> GetWhereAsync(Expression<Func<T, bool>> where, Expression<Func<T, object>> order, string orderEnum = "ascending", bool useCache = false, int cacheSecond = 3600);


        #endregion

        #region 修改操作

        /// <summary>
        /// 修改一条数据
        /// </summary>
        /// <param name="parm">T</param>
        /// <returns></returns>
        new int Update(T parm);
        new Task<int> UpdateAsync(T parm);

        /// <summary>
        /// 修改一条数据
        /// </summary>
        /// <param name="parm"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        int Update(T parm, Expression<Func<T, object>> columns);
        Task<int> UpdateAsync(T parm, Expression<Func<T, object>> columns);

        /// <summary>
        /// 批量修改
        /// </summary>
        /// <param name="parm">T</param>
        /// <returns></returns>
        int Update(List<T> parm);
        Task<int> UpdateAsync(List<T> parm);

        /// <summary>
        /// 批量修改
        /// </summary>
        /// <param name="parm"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        int Update(List<T> parm, Expression<Func<T, object>> columns);
        Task<int> UpdateAsync(List<T> parm, Expression<Func<T, object>> columns);

        /// <summary>
        /// 按查询条件更新
        /// </summary>
        /// <param name="where"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        int Update(Expression<Func<T, bool>> where, Expression<Func<T, T>> columns);
        Task<int> UpdateAsync(Expression<Func<T, bool>> where, Expression<Func<T, T>> columns);


        #endregion

        #region 删除操作

        /// <summary>
        /// 删除一条或多条数据
        /// </summary>
        /// <param name="parm">string</param>
        /// <returns></returns>
        int Delete(object id);
        Task<int> DeleteAsync(object id);


        /// <summary>
        /// 删除一条或多条数据
        /// </summary>
        /// <param name="parm">string</param>
        /// <returns></returns>
        int Delete(object[] ids);
        Task<int> DeleteAsync(object[] ids);

        /// <summary>
        /// 根据条件删除一条或多条数据
        /// </summary>
        /// <param name="where">过滤条件</param>
        /// <returns></returns>
        new Task<int> DeleteAsync(Expression<Func<T, bool>> where);

        int DeleteSplit(List<T> list);

        Task<int> DeleteSplitAsync(List<T> list);
        #endregion

        #region 添加或更新
        /// <summary>
        /// 添加或更新数据
        /// </summary>
        /// <param name="parm"><T></param>
        /// <returns></returns>
        int Saveable(T parm);
        Task<int> SaveableAsync(T parm);

        /// <summary>
        /// 批量添加或更新数据
        /// </summary>
        /// <param name="parm">List<T></param>
        /// <returns></returns>
        int Saveable(List<T> parm);
        Task<int> SaveableAsync(List<T> parm);

        /// <summary>
        /// 无主键添加或更新数据 (切记该表若有缓存，请执行 RemoveDataCache())
        /// </summary>
        /// <param name="parm"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        StorageableResult<T> Storageable(T parm, Expression<Func<T, object>> where);
        Task<StorageableResult<T>> StorageableAsync(T parm, Expression<Func<T, object>> where);

        /// <summary>
        /// 无主键添加或更新数据 (切记该表若有缓存，请执行 RemoveDataCache())
        /// </summary>
        /// <param name="parm"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        StorageableResult<T> Storageable(List<T> parm, Expression<Func<T, object>> where);
        Task<StorageableResult<T>> StorageableAsync(List<T> parm, Expression<Func<T, object>> where);
        #endregion

        Task<DbResult<bool>> UseITenantTran(Func<Task> action);
        Task<DbResult<bool>> UseAdoTran(Func<Task> action);
    }
}
