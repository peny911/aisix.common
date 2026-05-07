namespace Aisix.CodeFirst.Dialects
{
    internal sealed class MySqlDialect : IDatabaseDialect
    {
        public string QuoteIdentifier(string identifier)
        {
            return $"`{identifier.Replace("`", "``")}`";
        }

        public string BuildCreateIndexSql(string tableName, string indexName, IReadOnlyCollection<string> columns, bool isUnique)
        {
            var quotedColumns = string.Join(", ", columns.Select(QuoteIdentifier));
            var indexKeyword = isUnique ? "ADD UNIQUE INDEX" : "ADD INDEX";
            return $"ALTER TABLE {QuoteIdentifier(tableName)} {indexKeyword} {QuoteIdentifier(indexName)} ({quotedColumns})";
        }

        public string BuildSetColumnCommentSql(string tableName, string columnName, string comment, string? dbColumnType = null)
        {
            if (string.IsNullOrWhiteSpace(dbColumnType))
            {
                throw new ArgumentException("MySQL 同步字段注释时需要提供数据库字段类型。", nameof(dbColumnType));
            }

            var escapedComment = comment.Replace("'", "''");
            return $"ALTER TABLE {QuoteIdentifier(tableName)} MODIFY COLUMN {QuoteIdentifier(columnName)} {dbColumnType} COMMENT '{escapedComment}'";
        }
    }
}
