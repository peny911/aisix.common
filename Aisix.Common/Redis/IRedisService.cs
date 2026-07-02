using StackExchange.Redis;

namespace Aisix.Common.Redis
{
    public interface IRedisService
    {
        #region 同步方法
        /// <summary>
        /// 生产环境慎用
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        string[] Keys(string pattern, int? dbIndex = null);
        Dictionary<long, string[]> Scan(string pattern, long cursor, int count, int? dbIndex = null);
        TimeSpan Ping(int? dbIndex = null);
        bool Remove(string key, int? dbIndex = null);
        bool HashDelete(string key, string field, int? dbIndex = null);
        string? Get(string key, int? dbIndex = null);
        string?[] Get(string[] keys, int? dbIndex = null);
        bool Set(string keys, string values, int expireMinutes = 0, int? dbIndex = null);
        long SetAdd(string key, string[] values, int? dbIndex = null);
        long Increase(string key, long value = 1, int expireMinutes = 0, int? dbIndex = null);
        bool SetKeyExpire(string key, int expireMinutes, int? dbIndex = null);
        TimeSpan? GetKeyExpire(string key, int? dbIndex = null);
        string KeyRandom(int? dbIndex = null);
        bool KeyExists(string key, int? dbIndex = null);
        IList<T> HashGetAll<T>(string key, int? dbIndex = null);
        string?[]? HashGetAllFields(string key, int? dbIndex = null);
        int HashIncrement(string key, string field, long value, int? dbIndex = null);
        int HashIncrement(string key, string field, double value, int? dbIndex = null);
        int HashDecrement(string key, string field, int value, int expireMinutes = 0, int? dbIndex = null);
        bool HashSet(string key, string field, string value, int expireMinutes = 0, int? dbIndex = null);
        void HashSet(string key, List<HashEntry> fields, int expireMinutes = 0, int? dbIndex = null);
        HashEntry[] HashGetAll(string key, int? dbIndex = null);
        public List<decimal> HashValues(string key, int? dbIndex = null);
        string HashGet(string key, string field, int? dbIndex = null);
        string?[] HashGet(string key, string[] fields, int? dbIndex = null);
        bool HashRemove(string key, string field, int? dbIndex = null);
        long HashRemove(string key, string[] fields, int? dbIndex = null);
        string?[]? SetMembers(string key, int? dbIndex = null);
        string?[] SetRandomMembers(string key, int count = 9, int? dbIndex = null);
        bool SetRemove(string key, string member, int? dbIndex = null);
        /// <summary>
        /// 添加 Sorted Set member，可通过 When.NotExists 实现原子去重。
        /// </summary>
        bool SortedSetAdd(string key, string member, double score, When when = When.Always, int? dbIndex = null);
        /// <summary>
        /// 按 score 范围删除 Sorted Set member，用于清理过期窗口。
        /// </summary>
        long SortedSetRemoveRangeByScore(string key, double start, double stop, int? dbIndex = null);
        /// <summary>
        /// 删除指定 Sorted Set member，用于业务失败后的去重回滚。
        /// </summary>
        bool SortedSetRemove(string key, string member, int? dbIndex = null);
        long SetLength(string key, int? dbIndex = null);
        long ListLength(string key, int? dbIndex = null);
        #endregion

        #region 异步方法
        /// <summary>
        /// 生产环境慎用
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        Task<string[]> KeysAsync(string pattern, int? dbIndex = null);
        Task<Dictionary<long, string[]>> ScanAsync(string pattern, long cursor, int count, int? dbIndex = null);
        Task<List<string>> ScanAllKeysAsync(string pattern, int stepSize = 1000, int? dbIndex = null);
        Task<string[]> ScanAsync(string pattern, int? dbIndex = null);
        Task<TimeSpan> PingAsync(int? dbIndex = null);
        Task<bool> RemoveAsync(string key, int? dbIndex = null);
        Task<bool> HashDeleteAsync(string key, string field, int? dbIndex = null);
        Task<string?> GetAsync(string key, int? dbIndex = null);
        Task<string?[]> GetAsync(string[] keys, int? dbIndex = null);
        Task<bool> SetAsync(string key, string value, int expireMinutes = 0, int? dbIndex = null);
        Task<long> SetAddAsync(string key, string[] values, int? dbIndex = null);
        Task<long> IncreaseAsync(string key, long value = 1, int expireMinutes = 0, int? dbIndex = null);
        Task<double> IncreaseAsync(string key, double value = 1, int expireMinutes = 0, int? dbIndex = null);
        Task<bool> SetKeyExpireAsync(string key, int expireMinutes, int? dbIndex = null);
        Task<TimeSpan?> GetKeyExpireAsync(string key, int? dbIndex = null);
        Task<string> KeyRandomAsync(int? dbIndex = null);
        Task<bool> KeyExistsAsync(string key, int? dbIndex = null);
        Task<IList<T>> HashGetAllAsync<T>(string key, int? dbIndex = null);
        Task<string?[]?> HashGetAllFieldsAsync(string key, int? dbIndex = null);
        Task<long> HashIncrementAsync(string key, string field, long value, CommandFlags commandFlags = CommandFlags.None, int? dbIndex = null);
        Task<double> HashIncrementAsync(string key, string field, double value, int? dbIndex = null);
        Task<int> HashDecrementAsync(string key, string field, int value, int expireMinutes = 0, int? dbIndex = null);
        Task<bool> HashSetAsync(string key, string field, string value, int expireMinutes = 0, int? dbIndex = null);
        Task HashSetAsync(string key, List<HashEntry> fields, int expireMinutes = 0, int? dbIndex = null);
        Task<HashEntry[]> HashGetAllAsync(string key, int? dbIndex = null);
        Task<List<decimal>> HashValuesAsync(string key, int? dbIndex = null);
        Task<string> HashGetAsync(string key, string field, int? dbIndex = null);
        Task<string?[]?> HashGetAsync(string key, string[] fields, int? dbIndex = null);
        Task<bool> HashRemoveAsync(string key, string field, int? dbIndex = null);
        Task<long> HashRemoveAsync(string key, string[] fields, int? dbIndex = null);
        Task<string?[]?> SetMembersAsync(string key, int? dbIndex = null);
        Task<string?[]?> SetRandomMembersAsync(string key, int count = 9, int? dbIndex = null);
        Task<bool> SetRemoveAsync(string key, string member, int? dbIndex = null);
        /// <summary>
        /// 添加 Sorted Set member，可通过 When.NotExists 实现原子去重。
        /// </summary>
        Task<bool> SortedSetAddAsync(string key, string member, double score, When when = When.Always, int? dbIndex = null);
        /// <summary>
        /// 按 score 范围删除 Sorted Set member，用于清理过期窗口。
        /// </summary>
        Task<long> SortedSetRemoveRangeByScoreAsync(string key, double start, double stop, int? dbIndex = null);
        /// <summary>
        /// 删除指定 Sorted Set member，用于业务失败后的去重回滚。
        /// </summary>
        Task<bool> SortedSetRemoveAsync(string key, string member, int? dbIndex = null);
        Task<long> SetLengthAsync(string key, int? dbIndex = null);
        string ListGetByIndex(string key, long index, int? dbIndex = null);
        Task<string?[]?> ListRangeAsync(string key, long start = 0, long stop = -1, int? dbIndex = null);
        Task<string> ListGetByIndexAsync(string key, long index, int? dbIndex = null);
        Task<long> ListLengthAsync(string key, int? dbIndex = null);
        ITransaction CreateTransaction(int? dbIndex = null);
        IBatch CreateBatch(int? dbIndex = null);
        ISubscriber GetSubscriber();

        /// <summary>
        /// Set if Not Exists - 只有键不存在时才设置（原子操作）
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expireMinutes">过期时间（分钟），0 表示不过期</param>
        /// <param name="dbIndex">数据库索引</param>
        /// <returns>true=设置成功（键不存在），false=设置失败（键已存在）</returns>
        Task<bool> SetNxAsync(string key, string value, int expireMinutes = 0, int? dbIndex = null);

        #region HyperLogLog 方法
        /// <summary>
        /// 添加元素到 HyperLogLog（自动去重，固定内存约12KB）
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="values">值数组</param>
        /// <param name="dbIndex">数据库索引</param>
        /// <returns>如果至少添加了一个新元素返回 true</returns>
        Task<bool> HyperLogLogAddAsync(string key, string[] values, int? dbIndex = null);

        /// <summary>
        /// 获取 HyperLogLog 的基数估计值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="dbIndex">数据库索引</param>
        /// <returns>基数估计值（约 0.81% 误差率）</returns>
        Task<long> HyperLogLogLengthAsync(string key, int? dbIndex = null);
        #endregion

        #region Redis Stream 方法
        /// <summary>
        /// 添加 Redis Stream 消息，对应 XADD。
        /// </summary>
        /// <param name="key">Stream key。</param>
        /// <param name="values">写入 Stream 的字段和值。</param>
        /// <param name="messageId">消息 ID，默认由 Redis 自动生成。</param>
        /// <param name="maxLength">Stream 最大长度，设置后由 Redis 执行裁剪。</param>
        /// <param name="useApproximateMaxLength">是否使用近似裁剪。</param>
        /// <param name="commandFlags">Redis 命令标记。</param>
        /// <param name="dbIndex">数据库索引。</param>
        /// <returns>Redis 返回的 Stream 消息 ID。</returns>
        Task<RedisValue> StreamAddAsync(
            string key,
            NameValueEntry[] values,
            RedisValue? messageId = null,
            int? maxLength = null,
            bool useApproximateMaxLength = false,
            CommandFlags commandFlags = CommandFlags.None,
            int? dbIndex = null);

        /// <summary>
        /// 创建 Redis Stream 消费组，对应 XGROUP CREATE。
        /// </summary>
        /// <param name="key">Stream key。</param>
        /// <param name="groupName">消费组名称。</param>
        /// <param name="position">消费组起始位置，默认使用 StackExchange.Redis 默认值。</param>
        /// <param name="createStream">Stream 不存在时是否自动创建。</param>
        /// <param name="commandFlags">Redis 命令标记。</param>
        /// <param name="dbIndex">数据库索引。</param>
        /// <returns>创建成功返回 true。</returns>
        Task<bool> StreamCreateConsumerGroupAsync(
            string key,
            string groupName,
            RedisValue? position = null,
            bool createStream = true,
            CommandFlags commandFlags = CommandFlags.None,
            int? dbIndex = null);

        /// <summary>
        /// 使用消费组读取 Redis Stream 消息，对应 XREADGROUP。
        /// </summary>
        /// <param name="key">Stream key。</param>
        /// <param name="groupName">消费组名称。</param>
        /// <param name="consumerName">消费者名称。</param>
        /// <param name="position">读取位置，读取新消息通常传入 <c>&gt;</c> 或使用默认值。</param>
        /// <param name="count">最多读取消息数。</param>
        /// <param name="noAck">是否不进入 pending 且自动确认。</param>
        /// <param name="commandFlags">Redis 命令标记。</param>
        /// <param name="dbIndex">数据库索引。</param>
        /// <returns>读取到的 Stream 消息数组。</returns>
        Task<StreamEntry[]> StreamReadGroupAsync(
            string key,
            string groupName,
            string consumerName,
            RedisValue? position = null,
            int? count = null,
            bool noAck = false,
            CommandFlags commandFlags = CommandFlags.None,
            int? dbIndex = null);

        /// <summary>
        /// 确认 Redis Stream 消息，对应 XACK。
        /// </summary>
        /// <param name="key">Stream key。</param>
        /// <param name="groupName">消费组名称。</param>
        /// <param name="messageId">要确认的消息 ID。</param>
        /// <param name="commandFlags">Redis 命令标记。</param>
        /// <param name="dbIndex">数据库索引。</param>
        /// <returns>成功确认的消息数量。</returns>
        Task<long> StreamAcknowledgeAsync(
            string key,
            string groupName,
            RedisValue messageId,
            CommandFlags commandFlags = CommandFlags.None,
            int? dbIndex = null);

        /// <summary>
        /// 批量确认 Redis Stream 消息，对应 XACK。
        /// </summary>
        /// <param name="key">Stream key。</param>
        /// <param name="groupName">消费组名称。</param>
        /// <param name="messageIds">要确认的消息 ID 数组。</param>
        /// <param name="commandFlags">Redis 命令标记。</param>
        /// <param name="dbIndex">数据库索引。</param>
        /// <returns>成功确认的消息数量。</returns>
        Task<long> StreamAcknowledgeAsync(
            string key,
            string groupName,
            RedisValue[] messageIds,
            CommandFlags commandFlags = CommandFlags.None,
            int? dbIndex = null);

        /// <summary>
        /// 删除 Redis Stream 消息，对应 XDEL。
        /// </summary>
        /// <param name="key">Stream key。</param>
        /// <param name="messageIds">要删除的消息 ID 数组。</param>
        /// <param name="commandFlags">Redis 命令标记。</param>
        /// <param name="dbIndex">数据库索引。</param>
        /// <returns>成功删除的消息数量。</returns>
        Task<long> StreamDeleteAsync(
            string key,
            RedisValue[] messageIds,
            CommandFlags commandFlags = CommandFlags.None,
            int? dbIndex = null);
        #endregion
        #endregion
    }
}
