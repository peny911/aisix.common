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
        Task<long> SetLengthAsync(string key, int? dbIndex = null);
        string ListGetByIndex(string key, long index, int? dbIndex = null);
        Task<string?[]?> ListRangeAsync(string key, long start = 0, long stop = -1, int? dbIndex = null);
        Task<string> ListGetByIndexAsync(string key, long index, int? dbIndex = null);
        Task<long> ListLengthAsync(string key, int? dbIndex = null);
        ITransaction CreateTransaction(int? dbIndex = null);
        IBatch CreateBatch(int? dbIndex = null);

        /// <summary>
        /// Set if Not Exists - 只有键不存在时才设置（原子操作）
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expireMinutes">过期时间（分钟），0 表示不过期</param>
        /// <param name="dbIndex">数据库索引</param>
        /// <returns>true=设置成功（键不存在），false=设置失败（键已存在）</returns>
        Task<bool> SetNxAsync(string key, string value, int expireMinutes = 0, int? dbIndex = null);
        #endregion
    }
}
