using System;
using AElf.Types;
using Microsoft.Extensions.Caching.Memory;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IBlockExecutionDataCacheProvider
    {
        T Get<T>(Hash blockHash);
        void Add<T>(Hash blockHash, T data);
    }

    public class BlockExecutionDataCacheProvider : IBlockExecutionDataCacheProvider, ISingletonDependency
    {
        private readonly MemoryCache _dataCache;

        private const int ExpirationTime = 4000;

        public BlockExecutionDataCacheProvider()
        {
            _dataCache = new MemoryCache(new MemoryCacheOptions
            {
                ExpirationScanFrequency =
                    TimeSpan.FromMilliseconds(ExpirationTime)
            });
        }

        public T Get<T>(Hash blockHash)
        {
            if (_dataCache.TryGetValue(GetDataCacheKey<T>(blockHash), out var result))
            {
                return (T)result;
            }

            return default(T);
        }
        
        public void Add<T>(Hash blockHash, T data)
        {
            _dataCache.Set(GetDataCacheKey<T>(blockHash), data,
                TimeSpan.FromMilliseconds(ExpirationTime));
        }

        private string GetDataCacheKey<T>(Hash blockHash)
        {
            return typeof(T).FullName + blockHash.ToHex();
        }
    }
}