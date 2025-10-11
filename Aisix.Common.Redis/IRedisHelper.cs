using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aisix.Common.Redis
{
    public interface IRedisHelper
    {
        bool Remove(string key);
        bool HashDelete(string key, string field);
        string? Get(string key);
        Task<string?> GetAsync(string key);
        bool Set(string key, string value, int expireMinutes = 0);
        long SetAdd(string key, string[] values);
        Task<long> SetAddAsync(string key, string[] values);
        long Increase(string key, long value = 1, int expireMinutes = 0);
        bool SetKeyExpire(string key, int expireMinutes);
        TimeSpan? GetKeyExpire(string key);
        string KeyRandom();
        bool KeyExists(string key);
        IList<T> HashGetAll<T>(string key);
        string[] HashGetAllFields(string key);
        int HashIncrement(string key, string field, long value);
        Task<long> HashIncrementAsync(string key, string field, long value);
        int HashIncrement(string key, string field, double value);
        Task<double> HashIncrementAsync(string key, string field, double value);
        int HashDecrement(string key, string field, int value, int expireMinutes = 0);
        bool HashSet(string key, string field, string value, int expireMinutes = 0);
        void HashSet(string key, List<HashEntry> fields, int expireMinutes = 0);
        HashEntry[] HashGetAll(string key);
        string HashGet(string key, string field);
        string[] HashGet(string key, string[] fields);
        bool HashRemove(string key, string field);
        long HashRemove(string key, string[] fields);
        string[] SetMembers(string key);
        Task<string?[]> SetMembersAsync(string key);
        string[] SetRandomMembers(string key, int count = 9);
        bool SetRemove(string key, string member);
        long SetLength(string key);
        string ListGetByIndex(string key, long index);
        long ListLength(string key);
    }
}
