namespace Aisix.CodeFirst.Dialects
{
    internal interface IDatabaseDialect
    {
        string QuoteIdentifier(string identifier);

        string BuildCreateIndexSql(string tableName, string indexName, IReadOnlyCollection<string> columns, bool isUnique);

        string BuildSetTableCommentSql(string tableName, string comment);

        string BuildSetColumnCommentSql(string tableName, string columnName, string comment, string? dbColumnType = null);
    }
}
