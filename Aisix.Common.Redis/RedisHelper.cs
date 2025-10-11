using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aisix.Common.Redis
{

    public class RedisHelper : IRedisHelper
    {
        private static ConnectionMultiplexer redisConnection;
        private static readonly object padlock = new object();
        private static string connectionString = "localhost";

        private static ConnectionMultiplexer RedisConnection
        {
            get
            {
                if (redisConnection == null || !redisConnection.IsConnected)
                {
                    lock (padlock)
                    {
                        if (redisConnection == null || !redisConnection.IsConnected)
                        {
                            redisConnection = ConnectionMultiplexer.Connect(connectionString);
                        }
                    }
                }
                return redisConnection;
            }
        }

        public IDatabase GetDatabase()
        {
            return RedisConnection.GetDatabase();
        }

        #region Redis Utils
        public bool Remove(string key)
        {
            key = MergeKey(key);
            return GetDatabase().KeyDelete(key);
        }

        public bool HashDelete(string key, string field)
        {
            key = MergeKey(key);
            return GetDatabase().HashDelete(key, field);
        }

        public string? Get(string key)
        {
            key = MergeKey(key);
            return GetDatabase().StringGet(key);
        }

        public async Task<string?> GetAsync(string key)
        {
            key = MergeKey(key);
            return await GetDatabase().StringGetAsync(key);
        }

        public bool Set(string key, string value, int expireMinutes = 0)
        {
            key = MergeKey(key);
            var db = GetDatabase();

            return expireMinutes > 0
                ? db.StringSet(key, value, TimeSpan.FromMinutes(expireMinutes))
                : db.StringSet(key, value);
        }

        public long SetAdd(string key, string[] values)
        {
            key = MergeKey(key);
            var db = GetDatabase();
            var redisValues = Array.ConvertAll(values, item => (RedisValue)item);
            return db.SetAdd(key, redisValues);
        }

        public Task<long> SetAddAsync(string key, string[] values)
        {
            key = MergeKey(key);
            var db = GetDatabase();
            var redisValues = Array.ConvertAll(values, item => (RedisValue)item);
            return db.SetAddAsync(key, redisValues);
        }

        public long Increase(string key, long value = 1, int expireMinutes = 0)
        {
            if (!KeyExists(key))
            {
                Set(key, value.ToString(), expireMinutes);

                return value;
            }

            key = MergeKey(key);
            var db = GetDatabase();
            return db.StringIncrement(key, value);
        }

        public bool SetKeyExpire(string key, int expireMinutes)
        {
            if (expireMinutes <= 0)
            {
                return false;
            }

            key = MergeKey(key);
            return GetDatabase().KeyExpire(key, TimeSpan.FromMinutes(expireMinutes));
        }

        public TimeSpan? GetKeyExpire(string key)
        {
            key = MergeKey(key);
            return GetDatabase().KeyTimeToLive(key);
        }

        public string KeyRandom()
        {
            return GetDatabase().KeyRandom().ToString();
        }

        public bool KeyExists(string key)
        {
            key = MergeKey(key);
            return GetDatabase().KeyExists(key);
        }

        public IList<T> HashGetAll<T>(string key)
        {
            IList<T> result = new List<T>();
            key = MergeKey(key);
            HashEntry[] arr = GetDatabase().HashGetAll(key);

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

        public string[] HashGetAllFields(string key)
        {
            key = MergeKey(key);
            return GetDatabase().HashKeys(key).ToStringArray();
        }

        public int HashIncrement(string key, string field, long value)
        {
            //bool isNewKey = !KeyExists(key);
            var db = GetDatabase();

            int result = (int)db.HashIncrement(MergeKey(key), field, value);

            //if (isNewKey && expireMinutes > 0)
            //{
            //    SetKeyExpire(key, expireMinutes);
            //}

            return result;
        }

        public Task<long> HashIncrementAsync(string key, string field, long value)
        {
            return GetDatabase().HashIncrementAsync(MergeKey(key), field, value);
        }

        public int HashIncrement(string key, string field, double value)
        {
            var db = GetDatabase();

            int result = (int)db.HashIncrement(MergeKey(key), field, value);

            return result;
        }

        public Task<double> HashIncrementAsync(string key, string field, double value)
        {
            return GetDatabase().HashIncrementAsync(MergeKey(key), field, value);
        }

        public int HashDecrement(string key, string field, int value, int expireMinutes = 0)
        {
            bool isNewKey = !KeyExists(key);
            var db = GetDatabase();

            int result = (int)db.HashDecrement(MergeKey(key), field, value);

            if (isNewKey && expireMinutes > 0)
            {
                SetKeyExpire(key, expireMinutes);
            }

            return result;
        }

        public bool HashSet(string key, string field, string value, int expireMinutes = 0)
        {
            bool isNewKey = !KeyExists(key);
            var db = GetDatabase();

            var result = db.HashSet(MergeKey(key), field, value, When.NotExists);

            if (isNewKey && expireMinutes > 0)
            {
                SetKeyExpire(key, expireMinutes);
            }

            return result;
        }

        public void HashSet(string key, List<HashEntry> fields, int expireMinutes = 0)
        {
            bool isNewKey = !KeyExists(key);
            var db = GetDatabase();

            db.HashSet(MergeKey(key), fields.ToArray());

            if (isNewKey && expireMinutes > 0)
            {
                SetKeyExpire(key, expireMinutes);
            }
        }

        public HashEntry[] HashGetAll(string key)
        {
            var db = GetDatabase();
            var result = db.HashGetAll(MergeKey(key));
            return result;
        }

        public string HashGet(string key, string field)
        {
            var db = GetDatabase();
            var result = db.HashGet(MergeKey(key), field).ToString();
            return result;
        }

        public string[] HashGet(string key, string[] fields)
        {
            var db = GetDatabase();
            RedisValue[] hashFields = new RedisValue[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                hashFields[i] = new RedisValue(fields[i]);
            }
            var result = db.HashGet(MergeKey(key), hashFields).ToStringArray();
            return result;
        }

        public bool HashRemove(string key, string field)
        {
            bool isNewKey = !KeyExists(key);
            var db = GetDatabase();
            var result = db.HashDelete(MergeKey(key), field);
            return result;
        }

        public long HashRemove(string key, string[] fields)
        {
            RedisValue[] redisValue = new RedisValue[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                redisValue[i] = new RedisValue(fields[i]);
            }
            var db = GetDatabase();
            var result = db.HashDelete(MergeKey(key), redisValue);
            return result;
        }

        public string?[] SetMembers(string key)
        {
            key = MergeKey(key);
            var db = GetDatabase();
            return db.SetMembers(key).ToStringArray();
        }

        public async Task<string?[]> SetMembersAsync(string key)
        {
            key = MergeKey(key);
            var db = GetDatabase();
            var rvals = await db.SetMembersAsync(key);
            return rvals.ToStringArray();
        }

        /// <summary>
        /// 从Set中随机读取
        /// </summary>
        public string[] SetRandomMembers(string key, int count = 9)
        {
            key = MergeKey(key);
            var db = GetDatabase();
            return db.SetRandomMembers(key, count).ToStringArray();
        }

        public bool SetRemove(string key, string member)
        {
            key = MergeKey(key);
            var db = GetDatabase();
            return db.SetRemove(MergeKey(key), new RedisValue(member));
        }

        public long SetLength(string key)
        {
            key = MergeKey(key);

            var db = GetDatabase();
            return db.SetLength(key);
        }

        public string ListGetByIndex(string key, long index)
        {
            var db = GetDatabase();
            var result = db.ListGetByIndex(MergeKey(key), index).ToString();
            return result;
        }

        public long ListLength(string key)
        {
            return GetDatabase().ListLength(MergeKey(key));
        }
        #endregion

        #region Private methods
        private string MergeKey(string key)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(_prefix))
            {
                return key;
            }

            return $"{_prefix}:{key}";
        }
        //private IDatabase GetDatabase()
        //{
        //    return _connection.GetDatabase();
        //}
        #endregion
    }

}
