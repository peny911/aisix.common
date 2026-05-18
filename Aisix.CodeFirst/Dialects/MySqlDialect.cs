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

        public string BuildSetTableCommentSql(string tableName, string comment)
        {
            var escapedComment = comment.Replace("'", "''");
            return $"ALTER TABLE {QuoteIdentifier(tableName)} COMMENT = '{escapedComment}'";
        }

        public string BuildSetColumnCommentSql(string tableName, string columnName, string comment, string? fullColumnDefinition = null)
        {
            if (string.IsNullOrWhiteSpace(fullColumnDefinition))
            {
                throw new ArgumentException("MySQL 同步字段注释时需要提供完整的字段定义（含类型、长度、可空性等）。", nameof(fullColumnDefinition));
            }

            var escapedComment = comment.Replace("'", "''");
            return $"ALTER TABLE {QuoteIdentifier(tableName)} MODIFY COLUMN {QuoteIdentifier(columnName)} {fullColumnDefinition} COMMENT '{escapedComment}'";
        }
    }
}
