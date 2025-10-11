using Aisix.Common.Db.Repository;
using Aisix.Common.Model.Api;
using SqlSugar;
using System.Linq.Expressions;

namespace Aisix.Common.Db.Service
{
    public class BaseService<T> : BaseRepository<T>, IBaseService<T> /*ITransientDependency*/ where T : class, new()
    {
    }
}
