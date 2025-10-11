using SqlSugar;

namespace Aisix.Common.Db.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ISqlSugarClient _sqlSugarClient;
        private ITenant _tenant;

        public UnitOfWork(ISqlSugarClient sqlSugarClient)
        {
            _sqlSugarClient = sqlSugarClient;
            _tenant = _sqlSugarClient.AsTenant();
            Console.WriteLine("UnitOfWork ISqlSugarClient ContextID: " + sqlSugarClient.ContextID);
            Console.WriteLine("UnitOfWork ISqlSugarClient Hash: " + sqlSugarClient.GetHashCode());
            Console.WriteLine("UnitOfWork Current ConfigId: " + sqlSugarClient.CurrentConnectionConfig.ConfigId);
        }

        public ITenant GetTenant() => _tenant;

        public async Task BeginTransactionAsync()
        {
            await _tenant.BeginTranAsync();
        }

        public async Task CommitAsync()
        {
            await _tenant.CommitTranAsync();
        }

        public async Task RollbackAsync()
        {
            await _tenant.RollbackTranAsync();
        }
    }
}
