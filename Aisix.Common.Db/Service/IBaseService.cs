using Aisix.Common.Db.Repository;
using Aisix.Common.Model.Api;
using SqlSugar;
using System.Linq.Expressions;

namespace Aisix.Common.Db.Service
{
    public interface IBaseService<T> : IBaseRepository<T> where T : class, new()
    {
    }
}
