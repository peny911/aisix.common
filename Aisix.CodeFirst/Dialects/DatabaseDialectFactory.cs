using Aisix.Common.Db;

namespace Aisix.CodeFirst.Dialects
{
    internal static class DatabaseDialectFactory
    {
        public static IDatabaseDialect Create(DataBaseType dbType)
        {
            return dbType switch
            {
                DataBaseType.PostgreSQL => new PostgreSqlDialect(),
                _ => new MySqlDialect()
            };
        }
    }
}
