using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface IBlockchainExecutedDataCacheProvider<T>
    {
        bool TryGetBlockExecutedData(string key, out T value);
        void SetBlockExecutedData(string key, T blockExecutedData);
        void RemoveBlockExecutedData(string key);
        bool TryGetChangeHeight(string key, out long value);
        void SetChangeHeight(string key, long value);
        void CleanChangeHeight(long height);
    }

    public class BlockchainExecutedDataCacheProvider<T> : IBlockchainExecutedDataCacheProvider<T>
    {
        private readonly ConcurrentDictionary<string, T> _blockExecutedDataDic = new ConcurrentDictionary<string, T>();
        private readonly ConcurrentDictionary<string, long> _changeHeightDic = new ConcurrentDictionary<string, long>();

        public bool TryGetBlockExecutedData(string key, out T value)
        {
            return _blockExecutedDataDic.TryGetValue(key, out value);
        }

        public void SetBlockExecutedData(string key, T blockExecutedData)
        {
            _blockExecutedDataDic[key] = blockExecutedData;
        }

        public void RemoveBlockExecutedData(string key)
        {
            _blockExecutedDataDic.TryRemove(key, out _);
        }

        public bool TryGetChangeHeight(string key, out long value)
        {
            return _changeHeightDic.TryGetValue(key, out value);
        }

        public void SetChangeHeight(string key, long value)
        {
            _changeHeightDic[key] = value;
        }

        public void CleanChangeHeight(long height)
        {
            var keys = _changeHeightDic.Where(pair => pair.Value <= height).Select(pair => pair.Key);
            foreach (var key in keys)
            {
                _changeHeightDic.TryRemove(key, out _);
            }
        }
    }
}