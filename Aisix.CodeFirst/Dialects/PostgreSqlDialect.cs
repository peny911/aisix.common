namespace Aisix.CodeFirst.Dialects
{
    internal sealed class PostgreSqlDialect : IDatabaseDialect
    {
        public string QuoteIdentifier(string identifier)
        {
            return $"\"{identifier.Replace("\"", "\"\"")}\"";
        }

        public string BuildCreateIndexSql(string tableName, string indexName, IReadOnlyCollection<string> columns, bool isUnique)
        {
            var quotedColumns = string.Join(", ", columns.Select(QuoteIdentifier));
            var uniqueKeyword = isUnique ? "UNIQUE " : string.Empty;
            return $"CREATE {uniqueKeyword}INDEX IF NOT EXISTS {QuoteIdentifier(indexName)} ON {QuoteIdentifier(tableName)} ({quotedColumns})";
        }

        public string BuildSetColumnCommentSql(string tableName, string columnName, string comment, string? dbColumnType = null)
        {
            var escapedComment = comment.Replace("'", "''");
            return $"COMMENT ON COLUMN {QuoteIdentifier(tableName)}.{QuoteIdentifier(columnName)} IS '{escapedComment}'";
        }
    }
}
