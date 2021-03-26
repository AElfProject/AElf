// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Volo.Abp;
//
// #pragma warning disable 1998
//
// namespace AElf.Database
// {
//     public class RedisDatabase<TKeyValueDbContext> : IKeyValueDatabase<TKeyValueDbContext>
//         where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
//     {
//         private readonly SCRedisLite _pooledRedisLite;
//
//         public RedisDatabase(KeyValueDatabaseOptions<TKeyValueDbContext> options)
//         {
//             Check.NotNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
//             var endpoint = options.ConnectionString.ToRedisEndpoint();
//             _pooledRedisLite = new SCRedisLite(endpoint.Host, endpoint.Port, db: (int) endpoint.Db);
//         }
//
//         public async Task<bool> IsExistsAsync(string key)
//         {
//             Check.NotNullOrWhiteSpace(key, nameof(key));
//             return await _pooledRedisLite.ExistsAsync(key);
//         }
//
//         public async Task<bool> IsConnected()
//         {
//             return await _pooledRedisLite.PingAsync();
//         }
//
//         public async Task<byte[]> GetAsync(string key)
//         {
//             Check.NotNullOrWhiteSpace(key, nameof(key));
//             return await _pooledRedisLite.GetAsync(key);
//         }
//
//         public async Task SetAsync(string key, byte[] bytes)
//         {
//             Check.NotNullOrWhiteSpace(key, nameof(key));
//             await _pooledRedisLite.SetAsync(key, bytes);
//         }
//
//         public async Task RemoveAsync(string key)
//         {
//             Check.NotNullOrWhiteSpace(key, nameof(key));
//             await _pooledRedisLite.RemoveAsync(key);
//         }
//
//         public async Task SetAllAsync(IDictionary<string, byte[]> values)
//         {
//             if (values.Count == 0)
//                 return;
//             foreach (var key in values.Keys)
//             {
//                 Check.NotNullOrWhiteSpace(key, nameof(key));
//             }
//             
//             await _pooledRedisLite.SetAllAsync(values);
//         }
//         
//         public async Task<List<byte[]>> GetAllAsync(IList<string> keys)
//         {
//             if (keys.Count == 0)
//                 return null;
//             foreach (var key in keys)
//             {
//                 Check.NotNullOrWhiteSpace(key, nameof(key));
//             }
//
//             return (await _pooledRedisLite.GetAllAsync(keys.ToArray())).ToList();
//         }
//         
//         public async Task RemoveAllAsync(IList<string> keys)
//         {
//             if (keys.Count == 0)
//                 return;
//             foreach (var key in keys)
//             {
//                 Check.NotNullOrWhiteSpace(key, nameof(key));
//             }
//
//             await _pooledRedisLite.RemoveAllAsync(keys.ToArray());
//         }
//     }
// }