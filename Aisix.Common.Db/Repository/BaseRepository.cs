using Aisix.Common.Model.Api;
using SqlSugar;
using SqlSugar.IOC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Aisix.Common.Db.Repository
{
    public class BaseRepository<T> : SimpleClient<T>, IBaseRepository<T> where T : class, new()
    {
        public ITenant itenant = null;//多租户事务
        public BaseRepository(ISqlSugarClient context = null) : base(context)
        {
            // 不再尝试内部 new 或通过 DbScoped 获取
            var configId = typeof(T).GetCustomAttribute<TenantAttribute>()?.configId;
            if (configId != null)
            {
                //Context = context.AsTenant().GetConnectionScope(configId);
                Context = DbScoped.SugarScope.GetConnectionScope(configId);
            }
            else
            {
                Context = context ?? DbScoped.SugarScope;
            }

            itenant = DbScoped.SugarScope; // 确保使用与UnitOfWork一致的上下文
        }


        #region 添加操作
        /// <summary>
        /// 添加一条数据
        /// </summary>
        /// <param name="parm">T</param>
        /// <returns></returns>
        public int Add(T parm)
        {
            return Context.Insertable(parm).RemoveDataCache().ExecuteCommand();
        }

        public long AddReturnBigId(T parm)
        {
            return Context.Insertable(parm).RemoveDataCache().ExecuteReturnBigIdentity();
        }

        public async Task<int> AddAsync(T parm)
        {
            return await Context.Insertable(parm).RemoveDataCache().ExecuteCommandAsync();
        }

        public async Task<long> AddReturnBigIdAsync(T parm)
        {
            return await Context.Insertable(parm).RemoveDataCache().ExecuteReturnBigIdentityAsync();
        }

        /// <summary>
        /// 批量添加数据
        /// </summary>
        /// <param name="parm">List<T></param>
        /// <returns></returns>
        public int Add(List<T> parm)
        {
            return Context.Insertable(parm).RemoveDataCache().ExecuteCommand();
        }

        public async Task<int> AddAsync(List<T> parm)
        {
            return await Context.Insertable(parm).RemoveDataCache().ExecuteCommandAsync();
        }

        public async Task<List<long>> AddSplitReturnSnowflakeIdListAsync(List<T> parm)
        {
            return await Context.Insertable(parm).RemoveDataCache().SplitTable().ExecuteReturnSnowflakeIdListAsync();
        }

        #endregion

        #region 查询操作

        /// <summary>
        /// 根据条件查询数据是否存在
        /// </summary>
        /// <param name="where">条件表达式树</param>
        /// <returns></returns>
        public bool Any(Expression<Func<T, bool>> where)
        {
            return Context.Queryable<T>().Any(where);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> where)
        {
            return await Context.Queryable<T>().AnyAsync(where);
        }

        /// <summary>
        /// 根据条件合计字段
        /// </summary>
        /// <param name="where">条件表达式树</param>
        /// <returns></returns>
        public TResult Sum<TResult>(Expression<Func<T, bool>> where, Expression<Func<T, TResult>> field)
        {
            return Context.Queryable<T>().Where(where).Sum(field);
        }

        public async Task<TResult> SumAsync<TResult>(Expression<Func<T, bool>> where, Expression<Func<T, TResult>> field)
        {
            return await Context.Queryable<T>().Where(where).SumAsync(field);
        }

        /// <summary>
        /// 根据主值查询单条数据
        /// </summary>
        /// <param name="pkValue">主键值</param>
        /// <returns>泛型实体</returns>
        public T GetId(object pkValue)
        {
            return Context.Queryable<T>().InSingle(pkValue);
        }

        public async Task<T> GetIdAsync(object pkValue)
        {
            return await Context.Queryable<T>().InSingleAsync(pkValue);
        }

        /// <summary>
        /// 根据主键查询多条数据
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<T> GetIn(object[] ids)
        {
            return Context.Queryable<T>().In(ids).ToList();
        }

        public async Task<List<T>> GetInAsync(object[] ids)
        {
            return await Context.Queryable<T>().In(ids).ToListAsync();
        }

        /// <summary>
        /// 根据条件取条数
        /// </summary>
        /// <param name="where">条件表达式树</param>
        /// <returns></returns>
        public int GetCount(Expression<Func<T, bool>> where)
        {
            return Context.Queryable<T>().Count(where);
        }

        public async Task<int> GetCountAsync(Expression<Func<T, bool>> where)
        {
            return await Context.Queryable<T>().CountAsync(where);
        }

        /// <summary>
        /// 查询所有数据(无分页,请慎用)
        /// </summary>
        /// <returns></returns>
        public List<T> GetAll(bool useCache = false, int cacheSecond = 3600)
        {
            return Context.Queryable<T>().WithCacheIF(useCache, cacheSecond).ToList();
        }

        public async Task<List<T>> GetAllAsync(bool useCache = false, int cacheSecond = 3600)
        {
            return await Context.Queryable<T>().WithCacheIF(useCache, cacheSecond).ToListAsync();
        }

        /// <summary>
        /// 获得一条数据
        /// </summary>
        /// <param name="where">Expression<Func<T, bool>></param>
        /// <returns></returns>
        public T GetFirst(Expression<Func<T, bool>> where)
        {
            return Context.Queryable<T>().Where(where).First();
        }

        public async Task<T> GetFirstAsync(Expression<Func<T, bool>> where)
        {
            return await Context.Queryable<T>().Where(where).FirstAsync();
        }

        /// <summary>
        /// 获得一条数据
        /// </summary>
        /// <param name="parm">string</param>
        /// <returns></returns>
        public T GetFirst(string parm)
        {
            return Context.Queryable<T>().Where(parm).First();
        }

        public async Task<T> GetFirstAsync(string parm)
        {
            return await Context.Queryable<T>().Where(parm).FirstAsync();
        }


        /// <summary>
        /// 根据条件查询分页数据
        /// </summary>
        /// <param name="where"></param>
        /// <param name="parm"></param>
        /// <returns></returns>
        public PagedInfo<T> GetPages(Expression<Func<T, bool>> where, PageParm parm)
        {
            PagedInfo<T> page = new PagedInfo<T>();

            int totalNumber = 0, totalPage = 0;

            page.Data = Context.Queryable<T>().Where(where)
                .OrderByIF(!string.IsNullOrEmpty(parm.sort), $"{parm.order_by} {(parm.sort == "desc" ? "desc" : "asc")}")
                .ToPageList(parm.page_num.Value, parm.page_size.Value, ref totalNumber, ref totalPage);

            page.Pager.PageSize = parm.page_size.Value;
            page.Pager.PageNum = parm.page_num.Value;
            page.Pager.TotalNumber = totalNumber;
            page.Pager.totalPage = totalPage;

            return page;
        }

        public async Task<PagedInfo<T>> GetPagesAsync(Expression<Func<T, bool>> where, PageParm parm)
        {
            PagedInfo<T> page = new PagedInfo<T>();

            var totalNumber = new RefAsync<int>();
            var totalPage = new RefAsync<int>();

            page.Data = await Context.Queryable<T>().Where(where)
                .OrderByIF(!string.IsNullOrEmpty(parm.sort), $"{parm.order_by} {(parm.sort == "desc" ? "desc" : "asc")}")
                .ToPageListAsync(parm.page_num.Value, parm.page_size.Value, totalNumber, totalPage);

            page.Pager.PageSize = parm.page_size.Value;
            page.Pager.PageNum = parm.page_num.Value;
            page.Pager.TotalNumber = totalNumber;
            page.Pager.totalPage = totalPage;

            return page;
        }


        /// <summary>
        /// 根据条件查询数据
        /// </summary>
        /// <param name="where">条件表达式树</param>
        /// <returns></returns>
        public List<T> GetWhere(Expression<Func<T, bool>> where, bool useCache = false, int cacheSecond = 3600)
        {
            var query = Context.Queryable<T>().Where(where).WithCacheIF(useCache, cacheSecond);
            return query.ToList();
        }

        public async Task<List<T>> GetWhereAsync(Expression<Func<T, bool>> where, bool useCache = false, int cacheSecond = 3600)
        {
            var query = Context.Queryable<T>().Where(where).WithCacheIF(useCache, cacheSecond);
            return await query.ToListAsync();
        }

        /// <summary>
        /// 根据条件查询数据
        /// </summary>
        /// <param name="where">条件表达式树</param>
        /// <returns></returns>
        public List<T> GetWhere(Expression<Func<T, bool>> where, Expression<Func<T, object>> order, string orderEnum = "ascending", bool useCache = false, int cacheSecond = 3600)
        {
            var query = Context.Queryable<T>().Where(where).OrderByIF(orderEnum == "ascending", order, OrderByType.Asc).OrderByIF(orderEnum == "desc", order, OrderByType.Desc).WithCacheIF(useCache, cacheSecond);
            return query.ToList();
        }

        public async Task<List<T>> GetWhereAsync(Expression<Func<T, bool>> where, Expression<Func<T, object>> order, string orderEnum = "ascending", bool useCache = false, int cacheSecond = 3600)
        {
            var query = Context.Queryable<T>().Where(where).OrderByIF(orderEnum == "ascending", order, OrderByType.Asc).OrderByIF(orderEnum == "desc", order, OrderByType.Desc).WithCacheIF(useCache, cacheSecond);
            return await query.ToListAsync();
        }

        #endregion

        #region 修改操作

        /// <summary>
        /// 修改一条数据
        /// </summary>
        /// <param name="parm">T</param>
        /// <returns></returns>
        public int Update(T parm)
        {
            return Context.Updateable(parm).RemoveDataCache().ExecuteCommand();
        }

        public async Task<int> UpdateAsync(T parm)
        {
            return await Context.Updateable(parm).RemoveDataCache().ExecuteCommandAsync();
        }

        /// <summary>
        /// 修改一条数据
        /// </summary>
        /// <param name="parm">T</param>
        /// <returns></returns>
        public int Update(T parm, Expression<Func<T, object>> columns)
        {
            return Context.Updateable(parm).WhereColumns(columns).RemoveDataCache().ExecuteCommand();
        }

        public async Task<int> UpdateAsync(T parm, Expression<Func<T, object>> columns)
        {
            return await Context.Updateable(parm).WhereColumns(columns).RemoveDataCache().ExecuteCommandAsync();
        }

        /// <summary>
        /// 批量修改
        /// </summary>
        /// <param name="parm">T</param>
        /// <returns></returns>
        public int Update(List<T> parm)
        {
            return Context.Updateable(parm).RemoveDataCache().ExecuteCommand();
        }

        public async Task<int> UpdateAsync(List<T> parm)
        {
            return await Context.Updateable(parm).RemoveDataCache().ExecuteCommandAsync();
        }

        /// <summary>
        /// 批量修改
        /// </summary>
        /// <param name="parm">T</param>
        /// <returns></returns>
        public int Update(List<T> parm, Expression<Func<T, object>> columns)
        {
            return Context.Updateable(parm).WhereColumns(columns).RemoveDataCache().ExecuteCommand();
        }

        public async Task<int> UpdateAsync(List<T> parm, Expression<Func<T, object>> columns)
        {
            return await Context.Updateable(parm).WhereColumns(columns).RemoveDataCache().ExecuteCommandAsync();
        }

        /// <summary>
        /// 按查询条件更新
        /// </summary>
        /// <param name="where"></param>
        /// <param name="columns"></param>
        /// <returns></returns>
        public int Update(Expression<Func<T, bool>> where, Expression<Func<T, T>> columns)
        {
            return Context.Updateable<T>().SetColumns(columns).Where(where).RemoveDataCache().ExecuteCommand();
        }

        public async Task<int> UpdateAsync(Expression<Func<T, bool>> where, Expression<Func<T, T>> columns)
        {
            return await Context.Updateable<T>().SetColumns(columns).Where(where).RemoveDataCache().ExecuteCommandAsync();
        }

        #endregion

        #region 删除操作

        /// <summary>
        /// 删除一条或多条数据
        /// </summary>
        /// <param name="parm">string</param>
        /// <returns></returns>
        public int Delete(object id)
        {
            return Context.Deleteable<T>(id).RemoveDataCache().ExecuteCommand();
        }

        public async Task<int> DeleteAsync(object id)
        {
            return await Context.Deleteable<T>(id).RemoveDataCache().ExecuteCommandAsync();
        }

        /// <summary>
        /// 删除一条或多条数据
        /// </summary>
        /// <param name="parm">string</param>
        /// <returns></returns>
        public int Delete(object[] ids)
        {
            return Context.Deleteable<T>().In(ids).RemoveDataCache().ExecuteCommand();
        }

        public async Task<int> DeleteAsync(object[] ids)
        {
            return await Context.Deleteable<T>().In(ids).RemoveDataCache().ExecuteCommandAsync();
        }

        /// <summary>
        /// 根据条件删除一条或多条数据
        /// </summary>
        /// <param name="where">过滤条件</param>
        /// <returns></returns>
        public int Delete(Expression<Func<T, bool>> where)
        {
            return Context.Deleteable<T>().Where(where).RemoveDataCache().ExecuteCommand();
        }

        public async Task<int> DeleteAsync(Expression<Func<T, bool>> where)
        {
            return await Context.Deleteable<T>().Where(where).RemoveDataCache().ExecuteCommandAsync();
        }

        public int DeleteSplit(List<T> delList)
        {
            return Context.Deleteable(delList).RemoveDataCache().SplitTable().ExecuteCommand();
        }

        public async Task<int> DeleteSplitAsync(List<T> delList)
        {
            return await Context.Deleteable(delList).RemoveDataCache().SplitTable().ExecuteCommandAsync();
        }
        #endregion

        #region 添加或更新

        /// <summary>
        /// 添加或更新数据
        /// </summary>
        /// <param name="parm">List<T></param>
        /// <returns></returns>
        public int Saveable(T parm)
        {
            return Context.Storageable(parm).DefaultAddElseUpdate().ExecuteCommand();
        }

        public async Task<int> SaveableAsync(T parm)
        {
            var command = Context.Storageable(parm);

            return await command.DefaultAddElseUpdate().ExecuteCommandAsync();
        }

        /// <summary>
        /// 批量添加或更新数据
        /// </summary>
        /// <param name="parm">List<T></param>
        /// <returns></returns>
        public int Saveable(List<T> parm)
        {
            int result = 0;
            var command = Context.Storageable(parm)
                .SplitInsert(it => true)
                .SplitUpdate(it => it.Any())
                .ToStorage();

            result += command.AsInsertable.ExecuteCommand();
            result += command.AsUpdateable.ExecuteCommand();

            return result;
        }

        public async Task<int> SaveableAsync(List<T> parm)
        {
            int result = 0;
            var command = Context.Storageable(parm)
                .SplitInsert(it => true)
                .SplitUpdate(it => it.Any())
                .ToStorage();

            result += await command.AsInsertable.ExecuteCommandAsync();
            result += await command.AsUpdateable.ExecuteCommandAsync();

            return result;
        }

        /// <summary>
        /// 无主键添加或更新数据 (切记该表若有缓存，请执行 RemoveDataCache())
        /// </summary>
        /// <param name="parm"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public StorageableResult<T> Storageable(T parm, Expression<Func<T, object>> where)
        {
            var command = Context.Storageable(parm).WhereColumns(where).ToStorage();

            return command;
        }

        public async Task<StorageableResult<T>> StorageableAsync(T parm, Expression<Func<T, object>> where)
        {
            var command = await Context.Storageable(parm).WhereColumns(where).ToStorageAsync();

            return command;
        }

        /// <summary>
        /// 无主键添加或更新数据 (切记该表若有缓存，请执行 RemoveDataCache())
        /// </summary>
        /// <param name="parm"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public StorageableResult<T> Storageable(List<T> parm, Expression<Func<T, object>> where)
        {
            var command = Context.Storageable(parm).WhereColumns(where).ToStorage();

            return command;
        }

        public async Task<StorageableResult<T>> StorageableAsync(List<T> parm, Expression<Func<T, object>> where)
        {
            var command = await Context.Storageable(parm).WhereColumns(where).ToStorageAsync();

            return command;
        }
        #endregion

        #region 事物委托
        /// <summary>
        /// 多租户异常事物
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<DbResult<bool>> UseITenantTran(Func<Task> action)
        {
            var resultTran = await itenant.UseTranAsync(async () =>
            {
                await action();
            });
            return resultTran;

        }
        /// <summary>
        /// 同一对句事物处理
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<DbResult<bool>> UseAdoTran(Func<Task> action)
        {
            var resultTran = await Context.Ado.UseTranAsync(async () =>
            {
                await action();
            });
            return resultTran;
        }
        #endregion
    }
}
