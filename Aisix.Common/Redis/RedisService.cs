using Newtonsoft.Json;
using StackExchange.Redis;

namespace Aisix.Common.Redis
{
    public class RedisService : IRedisService, IDisposable
    {
        private readonly RedisSettings _settings;
        private readonly ConnectionMultiplexer _connection;
        private bool _disposed = false;

        public RedisService(RedisSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            
            if (string.IsNullOrEmpty(settings.ConnectionString))
                throw new ArgumentException("Redis connection string cannot be null or empty.", nameof(settings));
            
            string connectionString = settings.ConnectionString;
            
            // 如果不支持数据库切换，将DefaultDatabase添加到连接字符串中
            if (!settings.SupportDatabaseSwitching && settings.DefaultDatabase != 0)
            {
                var configOptions = ConfigurationOptions.Parse(connectionString);
                configOptions.DefaultDatabase = settings.DefaultDatabase;
                configOptions.ConnectTimeout = 5000;
                configOptions.SyncTimeout = 5000;
                configOptions.AbortOnConnectFail = false;
                configOptions.ConnectRetry = 3;
                _connection = ConnectionMultiplexer.Connect(configOptions);
            }
            else
            {
                var configOptions = ConfigurationOptions.Parse(connectionString);
                configOptions.ConnectTimeout = 5000;
                configOptions.SyncTimeout = 5000;
                configOptions.AbortOnConnectFail = false;
                configOptions.ConnectRetry = 3;
                _connection = ConnectionMultiplexer.Connect(configOptions);
            }
            
            _connection.ConnectionFailed += OnConnectionFailed;
            _connection.ConnectionRestored += OnConnectionRestored;
            _connection.ErrorMessage += OnErrorMessage;
        }

        private void OnConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            Console.WriteLine($"Redis connection failed: {e.FailureType} - {e.Exception?.Message}");
        }

        private void OnConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            Console.WriteLine($"Redis connection restored: {e.FailureType}");
        }

        private void OnErrorMessage(object sender, RedisErrorEventArgs e)
        {
            Console.WriteLine($"Redis error: {e.Message}");
        }

        #region Redis Utils
        public string[] Keys(string pattern, int? dbIndex = null)
        {
            var keys = GetDatabase(dbIndex).Execute("KEYS", pattern);

            return (string[]?)keys ?? Array.Empty<string>();
        }

        public async Task<string[]> KeysAsync(string pattern, int? dbIndex = null)
        {
            var keys = await GetDatabase(dbIndex).ExecuteAsync("KEYS", pattern);

            return (string[]?)keys ?? Array.Empty<string>();
        }

        public Dictionary<long, string[]> Scan(string pattern, long nextCursor, int count, int? dbIndex = null)
        {
            var result = GetDatabase(dbIndex).Execute("SCAN", nextCursor.ToString(), "MATCH", pattern, "COUNT", count);
            return ParseRedisResult(result);
        }

        public async Task<Dictionary<long, string[]>> ScanAsync(string pattern, long nextCursor, int count, int? dbIndex = null)
        {
            // var result = await GetDatabase(dbIndex).ExecuteAsync("SCAN @cursor 'MATCH', @pattern, 'COUNT', @count", new { cursor = nextCursor, pattern = pattern, count = count });
            var result = await GetDatabase(dbIndex).ExecuteAsync("SCAN", nextCursor.ToString(), "MATCH", pattern, "COUNT", count);
            return ParseRedisResult(result);
        }

        public async Task<List<string>> ScanAllKeysAsync(string pattern, int stepSize = 1000, int? dbIndex = null)
        {
            long cursor = 0;
            List<string> result = new();

            do
            {
                var scanResult = await this.ScanAsync(pattern, cursor, stepSize, dbIndex);

                if (scanResult != null && scanResult.Count > 0)
                {
                    cursor = scanResult.Keys.First();

                    if (scanResult.TryGetValue(cursor, out string[] keys))
                    {
                        foreach (var key in keys)
                        {
                            result.Add(key);
                        }
                    }
                }
            }
            while (cursor > 0);

            return result;
        }

        [Obsolete("此方法实现不正确，请使用 ScanAsync(pattern, cursor, count) 或 ScanAllKeysAsync")]
        public async Task<string[]> ScanAsync(string pattern, int? dbIndex = null)
        {
            var keys = await GetDatabase(dbIndex).ScriptEvaluateAsync($"return redis.call('SCAN', 0, 'MATCH', '{pattern}')");

            return (string[]?)keys ?? Array.Empty<string>();
        }

        public TimeSpan Ping(int? dbIndex = null)
        {
            return GetDatabase(dbIndex).Ping();
        }
        public async Task<TimeSpan> PingAsync(int? dbIndex = null)
        {
            return await GetDatabase(dbIndex).PingAsync();
        }

        public bool Remove(string key, int? dbIndex = null)
        {
            key = MergeKey(key);
            return GetDatabase(dbIndex).KeyDelete(key);
        }
        public async Task<bool> RemoveAsync(string key, int? dbIndex = null)
        {
            key = MergeKey(key);
            return await GetDatabase(dbIndex).KeyDeleteAsync(key);
        }

        public bool HashDelete(string key, string field, int? dbIndex = null)
        {
            key = MergeKey(key);
            return GetDatabase(dbIndex).HashDelete(key, field);
        }
        public async Task<bool> HashDeleteAsync(string key, string field, int? dbIndex = null)
        {
            key = MergeKey(key);
            return await GetDatabase(dbIndex).HashDeleteAsync(key, field);
        }

        public string? Get(string key, int? dbIndex = null)
        {
            key = MergeKey(key);
            return GetDatabase(dbIndex).StringGet(key);
        }

        public async Task<string?> GetAsync(string key, int? dbIndex = null)
        {
            key = MergeKey(key);
            return await GetDatabase(dbIndex).StringGetAsync(key);
        }

        public string?[] Get(string[] keys, int? dbIndex = null)
        {
            RedisKey[] redisKeys = new RedisKey[keys.Length];

            for (int i = 0; i < keys.Length; i++)
            {
                redisKeys[i] = MergeKey(keys[i]);
            }

            RedisValue[] redisValues = GetDatabase(dbIndex).StringGet(redisKeys);

            return redisValues.ToStringArray();
        }

        public async Task<string?[]> GetAsync(string[] keys, int? dbIndex = null)
        {
            RedisKey[] redisKeys = new RedisKey[keys.Length];

            for (int i = 0; i < keys.Length; i++)
            {
                redisKeys[i] = MergeKey(keys[i]);
            }

            RedisValue[] redisValues = await GetDatabase(dbIndex).StringGetAsync(redisKeys);

            return redisValues.ToStringArray();
        }

        public bool Set(string key, string value, int expireMinutes = 0, int? dbIndex = null)
        {
            key = MergeKey(key);
            var db = GetDatabase(dbIndex);

            return expireMinutes > 0
                ? db.StringSet(key, value, TimeSpan.FromMinutes(expireMinutes))
                : db.StringSet(key, value);
        }

        public async Task<bool> SetAsync(string key, string value, int expireMinutes = 0, int? dbIndex = null)
        {
            key = MergeKey(key);
            var db = GetDatabase(dbIndex);

            return expireMinutes > 0
                ? await db.StringSetAsync(key, value, TimeSpan.FromMinutes(expireMinutes))
                : await db.StringSetAsync(key, value);
        }

        public long SetAdd(string key, string[] values, int? dbIndex = null)
        {
            key = MergeKey(key);
            var db = GetDatabase(dbIndex);
            var redisValues = Array.ConvertAll(values, item => (RedisValue)item);
            return db.SetAdd(key, redisValues);
        }

        public Task<long> SetAddAsync(string key, string[] values, int? dbIndex = null)
        {
            key = MergeKey(key);
            var db = GetDatabase(dbIndex);
            var redisValues = Array.ConvertAll(values, item => (RedisValue)item);
            return db.SetAddAsync(key, redisValues);
        }

        public long Increase(string key, long value = 1, int expireMinutes = 0, int? dbIndex = null)
        {
            if (!KeyExists(key, dbIndex))
            {
                Set(key, value.ToString(), expireMinutes, dbIndex);

                return value;
            }

            key = MergeKey(key);
            var db = GetDatabase(dbIndex);
            return db.StringIncrement(key, value);
        }
        public async Task<long> IncreaseAsync(string key, long value = 1, int expireMinutes = 0, int? dbIndex = null)
        {
            if (!await KeyExistsAsync(key, dbIndex))
            {
                await SetAsync(key, value.ToString(), expireMinutes, dbIndex);

                return value;
            }

            key = MergeKey(key);
            var db = GetDatabase(dbIndex);
            return await db.StringIncrementAsync(key, value);
        }
        public async Task<double> IncreaseAsync(string key, double value = 1, int expireMinutes = 0, int? dbIndex = null)
        {
            if (!await KeyExistsAsync(key, dbIndex))
            {
                await SetAsync(key, value.ToString(), expireMinutes, dbIndex);

                return value;
            }

            key = MergeKey(key);
            var db = GetDatabase(dbIndex);
            return await db.StringIncrementAsync(key, value);
        }

        public bool SetKeyExpire(string key, int expireMinutes, int? dbIndex = null)
        {
            if (expireMinutes <= 0)
            {
                return false;
            }

            key = MergeKey(key);
            return GetDatabase(dbIndex).KeyExpire(key, TimeSpan.FromMinutes(expireMinutes));
        }
        public async Task<bool> SetKeyExpireAsync(string key, int expireMinutes, int? dbIndex = null)
        {
            if (expireMinutes <= 0)
            {
                return false;
            }

            key = MergeKey(key);
            return await GetDatabase(dbIndex).KeyExpireAsync(key, TimeSpan.FromMinutes(expireMinutes));
        }

        public TimeSpan? GetKeyExpire(string key, int? dbIndex = null)
        {
            key = MergeKey(key);
            return GetDatabase(dbIndex).KeyTimeToLive(key);
        }

        public async Task<TimeSpan?> GetKeyExpireAsync(string key, int? dbIndex = null)
        {
            key = MergeKey(key);
            return await GetDatabase(dbIndex).KeyTimeToLiveAsync(key);
        }

        public string KeyRandom(int? dbIndex = null)
        {
            return GetDatabase(dbIndex).KeyRandom().ToString();
        }

        public async Task<string> KeyRandomAsync(int? dbIndex = null)
        {
            var result = await GetDatabase(dbIndex).KeyRandomAsync();
            return result.ToString();
        }

        public bool KeyExists(string key, int? dbIndex = null)
        {
            key = MergeKey(key);
            return GetDatabase(dbIndex).KeyExists(key);
        }
        public async Task<bool> KeyExistsAsync(string key, int? dbIndex = null)
        {
            key = MergeKey(key);
            return await GetDatabase(dbIndex).KeyExistsAsync(key);
        }

        public IList<T> HashGetAll<T>(string key, int? dbIndex = null)
        {
            IList<T> result = new List<T>();
            key = MergeKey(key);
            HashEntry[] arr = GetDatabase(dbIndex).HashGetAll(key);

            foreach (var item in arr)
            {
                if (!item.Value.IsNullOrEmpty)
                {
                    var obj = JsonConvert.DeserializeObject<T>(item.Value);
                    result.Add(obj);
                }
            }

            return result;
        }
        public async Task<IList<T>> HashGetAllAsync<T>(string key, int? dbIndex = null)
        {
            IList<T> result = new List<T>();
            key = MergeKey(key);
            HashEntry[] arr = await GetDatabase(dbIndex).HashGetAllAsync(key);

            foreach (var item in arr)
            {
                if (!item.Value.IsNullOrEmpty)
                {
                    var obj = JsonConvert.DeserializeObject<T>(item.Value);
                    result.Add(obj);
                }
            }

            return result;
        }

        public string?[]? HashGetAllFields(string key, int? dbIndex = null)
        {
            key = MergeKey(key);
            return GetDatabase(dbIndex).HashKeys(key).ToStringArray();
        }
        public async Task<string?[]?> HashGetAllFieldsAsync(string key, int? dbIndex = null)
        {
            key = MergeKey(key);
            var result = await GetDatabase(dbIndex).HashKeysAsync(key);
            return result.ToStringArray();
        }

        public int HashIncrement(string key, string field, long value, int? dbIndex = null)
        {
            return (int)GetDatabase(dbIndex).HashIncrement(MergeKey(key), field, value);
        }

        public Task<long> HashIncrementAsync(string key, string field, long value, CommandFlags commandFlags = CommandFlags.None, int? dbIndex = null)
        {
            return GetDatabase(dbIndex).HashIncrementAsync(MergeKey(key), field, value, commandFlags);
        }

        public int HashIncrement(string key, string field, double value, int? dbIndex = null)
        {
            return (int)GetDatabase(dbIndex).HashIncrement(MergeKey(key), field, value);
        }

        public Task<double> HashIncrementAsync(string key, string field, double value, int? dbIndex = null)
        {
            return GetDatabase(dbIndex).HashIncrementAsync(MergeKey(key), field, value);
        }

        public int HashDecrement(string key, string field, int value, int expireMinutes = 0, int? dbIndex = null)
        {
            bool isNewKey = !KeyExists(key, dbIndex);
            int result = (int)GetDatabase(dbIndex).HashDecrement(MergeKey(key), field, value);

            if (isNewKey && expireMinutes > 0)
            {
                SetKeyExpire(key, expireMinutes, dbIndex);
            }

            return result;
        }

        public async Task<int> HashDecrementAsync(string key, string field, int value, int expireMinutes = 0, int? dbIndex = null)
        {
            bool isNewKey = !await KeyExistsAsync(key, dbIndex);
            var db = GetDatabase(dbIndex);

            int result = (int)await db.HashDecrementAsync(MergeKey(key), field, value);

            if (isNewKey && expireMinutes > 0)
            {
                await SetKeyExpireAsync(key, expireMinutes, dbIndex);
            }

            return result;
        }

        public bool HashSet(string key, string field, string value, int expireMinutes = 0, int? dbIndex = null)
        {
            bool isNewKey = !KeyExists(key, dbIndex);
            var db = GetDatabase(dbIndex);

            var result = db.HashSet(MergeKey(key), field, value, When.NotExists);

            if (isNewKey && expireMinutes > 0)
            {
                SetKeyExpire(key, expireMinutes, dbIndex);
            }

            return result;
        }
        public async Task<bool> HashSetAsync(string key, string field, string value, int expireMinutes = 0, int? dbIndex = null)
        {
            bool isNewKey = !await KeyExistsAsync(key, dbIndex);
            var db = GetDatabase(dbIndex);

            var result = await db.HashSetAsync(MergeKey(key), field, value, When.NotExists);

            if (isNewKey && expireMinutes > 0)
            {
                await SetKeyExpireAsync(key, expireMinutes, dbIndex);
            }

            return result;
        }

        public void HashSet(string key, List<HashEntry> fields, int expireMinutes = 0, int? dbIndex = null)
        {
            bool isNewKey = !KeyExists(key, dbIndex);
            var db = GetDatabase(dbIndex);

            db.HashSet(MergeKey(key), fields.ToArray());

            if (isNewKey && expireMinutes > 0)
            {
                SetKeyExpire(key, expireMinutes, dbIndex);
            }
        }
        public async Task HashSetAsync(string key, List<HashEntry> fields, int expireMinutes = 0, int? dbIndex = null)
        {
            bool isNewKey = !await KeyExistsAsync(key, dbIndex);
            var db = GetDatabase(dbIndex);

            await db.HashSetAsync(MergeKey(key), fields.ToArray());

            if (isNewKey && expireMinutes > 0)
            {
                await SetKeyExpireAsync(key, expireMinutes, dbIndex);
            }
        }

        public HashEntry[] HashGetAll(string key, int? dbIndex = null)
        {
            var db = GetDatabase(dbIndex);
            var result = db.HashGetAll(MergeKey(key));
            return result;
        }
        public async Task<HashEntry[]> HashGetAllAsync(string key, int? dbIndex = null)
        {
            var db = GetDatabase(dbIndex);
            var result = await db.HashGetAllAsync(MergeKey(key));
            return result;
        }

        public List<decimal> HashValues(string key, int? dbIndex = null)
        {
            var db = GetDatabase(dbIndex);
            var result = db.HashValues(MergeKey(key)).Select(it => Convert.ToDecimal(it)).ToList();
            return result;
        }
        public async Task<List<decimal>> HashValuesAsync(string key, int? dbIndex = null)
        {
            var db = GetDatabase(dbIndex);
            var redisResult = await db.HashValuesAsync(MergeKey(key));
            var result = redisResult.Select(it => Convert.ToDecimal(it)).ToList();
            return result;
        }

        public string HashGet(string key, string field, int? dbIndex = null)
        {
            var db = GetDatabase(dbIndex);
            var result = db.HashGet(MergeKey(key), field).ToString();
            return result;
        }
        public async Task<string> HashGetAsync(string key, string field, int? dbIndex = null)
        {
            var db = GetDatabase(dbIndex);
            var result = await db.HashGetAsync(MergeKey(key), field);
            return result.ToString();
        }

        public string[] HashGet(string key, string[] fields, int? dbIndex = null)
        {
            var db = GetDatabase(dbIndex);
            RedisValue[] hashFields = new RedisValue[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                hashFields[i] = new RedisValue(fields[i]);
            }
            var result = db.HashGet(MergeKey(key), hashFields).ToStringArray();
            return result;
        }

        public async Task<string?[]?> HashGetAsync(string key, string[] fields, int? dbIndex = null)
        {
            var db = GetDatabase(dbIndex);
            RedisValue[] hashFields = new RedisValue[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                hashFields[i] = new RedisValue(fields[i]);
            }
            var result = await db.HashGetAsync(MergeKey(key), hashFields);
            return result.ToStringArray(); ;
        }

        public bool HashRemove(string key, string field, int? dbIndex = null)
        {
            bool isNewKey = !KeyExists(key, dbIndex);
            var db = GetDatabase(dbIndex);
            var result = db.HashDelete(MergeKey(key), field);
            return result;
        }
        public async Task<bool> HashRemoveAsync(string key, string field, int? dbIndex = null)
        {
            bool isNewKey = !await KeyExistsAsync(key, dbIndex);
            var db = GetDatabase(dbIndex);
            var result = await db.HashDeleteAsync(MergeKey(key), field);
            return result;
        }

        public long HashRemove(string key, string[] fields, int? dbIndex = null)
        {
            RedisValue[] redisValue = new RedisValue[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                redisValue[i] = new RedisValue(fields[i]);
            }
            var db = GetDatabase(dbIndex);
            var result = db.HashDelete(MergeKey(key), redisValue);
            return result;
        }
        public async Task<long> HashRemoveAsync(string key, string[] fields, int? dbIndex = null)
        {
            RedisValue[] redisValue = new RedisValue[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                redisValue[i] = new RedisValue(fields[i]);
            }
            var db = GetDatabase(dbIndex);
            var result = await db.HashDeleteAsync(MergeKey(key), redisValue);
            return result;
        }

        public string?[]? SetMembers(string key, int? dbIndex = null)
        {
            key = MergeKey(key);
            var db = GetDatabase(dbIndex);
            return db.SetMembers(key).ToStringArray();
        }
        public async Task<string?[]?> SetMembersAsync(string key, int? dbIndex = null)
        {
            key = MergeKey(key);
            var db = GetDatabase(dbIndex);
            var result = await db.SetMembersAsync(key);
            return result.ToStringArray();
        }

        /// <summary>
        /// 从Set中随机读取
        /// </summary>
        public string[] SetRandomMembers(string key, int count = 9, int? dbIndex = null)
        {
            key = MergeKey(key);
            var db = GetDatabase(dbIndex);
            return db.SetRandomMembers(key, count).ToStringArray();
        }
        public async Task<string?[]?> SetRandomMembersAsync(string key, int count = 9, int? dbIndex = null)
        {
            key = MergeKey(key);
            var db = GetDatabase(dbIndex);
            var result = await db.SetRandomMembersAsync(key, count);
            return result.ToStringArray();
        }

        public bool SetRemove(string key, string member, int? dbIndex = null)
        {
            key = MergeKey(key);
            var db = GetDatabase(dbIndex);
            return db.SetRemove(MergeKey(key), new RedisValue(member));
        }
        public async Task<bool> SetRemoveAsync(string key, string member, int? dbIndex = null)
        {
            key = MergeKey(key);
            var db = GetDatabase(dbIndex);
            return await db.SetRemoveAsync(MergeKey(key), new RedisValue(member));
        }

        public long SetLength(string key, int? dbIndex = null)
        {
            key = MergeKey(key);

            var db = GetDatabase(dbIndex);
            return db.SetLength(key);
        }
        public async Task<long> SetLengthAsync(string key, int? dbIndex = null)
        {
            key = MergeKey(key);

            var db = GetDatabase(dbIndex);
            return await db.SetLengthAsync(key);
        }

        public string ListGetByIndex(string key, long index, int? dbIndex = null)
        {
            var db = GetDatabase(dbIndex);
            var result = db.ListGetByIndex(MergeKey(key), index).ToString();
            return result;
        }
        public async Task<string?[]?> ListRangeAsync(string key, long start = 0, long stop = -1, int? dbIndex = null)
        {
            var db = GetDatabase(dbIndex);
            var result = await db.ListRangeAsync(MergeKey(key), start, stop);
            return result.ToStringArray();
        }

        public async Task<string> ListGetByIndexAsync(string key, long index, int? dbIndex = null)
        {
            var db = GetDatabase(dbIndex);
            var result = await db.ListGetByIndexAsync(MergeKey(key), index);
            return result.ToString();
        }

        public long ListLength(string key, int? dbIndex = null)
        {
            return GetDatabase(dbIndex).ListLength(MergeKey(key));
        }
        public async Task<long> ListLengthAsync(string key, int? dbIndex = null)
        {
            return await GetDatabase(dbIndex).ListLengthAsync(MergeKey(key));
        }
        #endregion

        #region Private methods
        private string MergeKey(string key)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(_settings.DefaultPrefix))
            {
                return key;
            }

            return $"{_settings.DefaultPrefix}:{key}";
        }

        //private IDatabase GetDatabase(dbIndex)
        //{
        //    return _connection.GetDatabase(dbIndex);
        //}

        private IDatabase GetDatabase(int? dbIndex = null)
        {
            if (_settings.SupportDatabaseSwitching)
            {
                // 支持数据库切换，可以动态指定数据库
                return dbIndex.HasValue ? _connection.GetDatabase(dbIndex.Value) : _connection.GetDatabase(_settings.DefaultDatabase);
            }
            else
            {
                // 不支持数据库切换，只能使用连接时指定的数据库
                if (dbIndex.HasValue && dbIndex.Value != _settings.DefaultDatabase)
                {
                    throw new NotSupportedException($"Redis cluster mode does not support database switching. Current database: {_settings.DefaultDatabase}, requested: {dbIndex.Value}");
                }
                return _connection.GetDatabase(_settings.DefaultDatabase);
            }
        }

        private Dictionary<long, string[]> ParseRedisResult(RedisResult result)
        {
            var parsedResult = new Dictionary<long, string[]>();

            if (result.IsNull)
            {
                return parsedResult;
            }

            RedisResult[] results = (RedisResult[])result;

            // 第一个元素是新的游标
            long newCursor = long.Parse(results[0].ToString());

            // 第二个元素是键的数组
            RedisResult[] keys = (RedisResult[])results[1];
            string[] keyStrings = Array.ConvertAll(keys, key => (string)key);

            parsedResult.Add(newCursor, keyStrings);

            return parsedResult;
        }

        public ITransaction CreateTransaction(int? dbIndex = null)
        {
            return GetDatabase(dbIndex).CreateTransaction();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_connection != null)
                    {
                        _connection.ConnectionFailed -= OnConnectionFailed;
                        _connection.ConnectionRestored -= OnConnectionRestored;
                        _connection.ErrorMessage -= OnErrorMessage;
                        _connection.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        ~RedisService()
        {
            Dispose(false);
        }
        #endregion
    }
}
