using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContractExecution.Domain;
using AElf.Kernel.Types;
using AElf.SmartContract;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus
{
    /// <summary>
    /// Reads contract data from state store.
    /// </summary>
    public class ConsensusDataReader : ISingletonDependency
    {
        private readonly IStateManager _stateManager;

        public ConsensusDataReader(IStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        private DataProvider GetDataProvider(int chainId)
        {
            var dp = DataProvider.GetRootDataProvider(chainId, ContractHelpers.GetConsensusContractAddress(chainId));
            dp.StateManager = _stateManager;
            return dp;
        }

        public async Task<byte[]> ReadFieldAsync<T>(int chainId, Hash keyHash) where T : IMessage, new()
        {
            return await GetDataProvider(chainId).GetAsync<T>(keyHash);
        }
        
        public async Task<byte[]> ReadFieldAsync<T>(int chainId, string key) where T : IMessage, new()
        {
            return await ReadFieldAsync<T>(chainId, Hash.FromString(key));
        }
        
        public byte[] ReadFiled<T>(int chainId, string key) where T : IMessage, new()
        {
            return ReadFieldAsync<T>(chainId, Hash.FromString(key)).Result;
        }
        
        public async Task<byte[]> ReadMapAsync<T>(int chainId, Hash keyHash, string resourceStr) where T : IMessage, new()
        {
            return await GetDataProvider(chainId).GetChild(resourceStr).GetAsync<T>(keyHash);
        }
        
        public async Task<byte[]> ReadMapAsync<T>(int chainId, string key, string resourceStr) where T : IMessage, new()
        {
            return await ReadMapAsync<T>(chainId, Hash.LoadHex(key), resourceStr);
        }
        
        public byte[] ReadMap<T>(int chainId, Hash keyHash, string resourceStr) where T : IMessage, new()
        {
            return ReadMapAsync<T>(chainId, keyHash, resourceStr).Result;
        }
        
        public byte[] ReadMap<T>(int chainId, IMessage message, string resourceStr) where T : IMessage, new()
        {
            return ReadMapAsync<T>(chainId, Hash.FromMessage(message).ToHex(), resourceStr).Result;
        }

        public byte[] ReadMap<T>(int chainId, string key, string resourceStr) where T : IMessage, new()
        {
            return ReadMapAsync<T>(chainId, Hash.FromString(key), resourceStr).Result;
        }
    }
}