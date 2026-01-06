namespace Aisix.DbFirst.Services
{
    /// <summary>
    /// DbFirst 服务接口
    /// </summary>
    public interface IDbFirstService
    {
        /// <summary>
        /// 运行 DbFirst 代码生成
        /// </summary>
        void Run();

        /// <summary>
        /// 获取所有可用的表
        /// </summary>
        List<string> GetAllTables();

        /// <summary>
        /// 生成指定表的代码
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>生成结果</returns>
        GenerateResult GenerateTable(string tableName);

        /// <summary>
        /// 批量生成表的代码
        /// </summary>
        /// <param name="tableNames">表名列表</param>
        /// <returns>生成结果列表</returns>
        List<GenerateResult> GenerateTables(List<string> tableNames);
    }

    /// <summary>
    /// 代码生成结果
    /// </summary>
    public class GenerateResult
    {
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 实体类生成结果
        /// </summary>
        public bool EntityGenerated { get; set; }

        /// <summary>
        /// 服务实现生成结果
        /// </summary>
        public bool ServiceGenerated { get; set; }

        /// <summary>
        /// 服务接口生成结果
        /// </summary>
        public bool IServiceGenerated { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
