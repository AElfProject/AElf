using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel.Storages;
using AElf.SmartContract;
using Google.Protobuf;

namespace AElf.Kernel.Consensus
{
    /// <summary>
    /// Reads contract data from state store.
    /// </summary>
    public class ConsensusDataReader
    {
        private static Hash ChainId => Hash.LoadBase58(ChainConfig.Instance.ChainId);
        private static Address ContractAddress => ContractHelpers.GetConsensusContractAddress(ChainId);
        
        private readonly IStateStore _stateStore;

        public ConsensusDataReader(IStateStore stateStore)
        {
            _stateStore = stateStore;
        }

        private DataProvider DataProvider
        {
            get
            {
                var dp = DataProvider.GetRootDataProvider(ChainId, ContractAddress);
                dp.StateStore = _stateStore;
                return dp;
            }
        }

        public async Task<byte[]> ReadFieldAsync<T>(Hash keyHash) where T : IMessage, new()
        {
            return await DataProvider.GetAsync<T>(keyHash);
        }
        
        public async Task<byte[]> ReadFieldAsync<T>(string key) where T : IMessage, new()
        {
            return await ReadFieldAsync<T>(Hash.FromString(key));
        }
        
        public byte[] ReadFiled<T>(string key) where T : IMessage, new()
        {
            return ReadFieldAsync<T>(Hash.FromString(key)).Result;
        }
        
        public async Task<byte[]> ReadMapAsync<T>(Hash keyHash, string resourceStr) where T : IMessage, new()
        {
            return await DataProvider.GetChild(resourceStr).GetAsync<T>(keyHash);
        }
        
        public async Task<byte[]> ReadMapAsync<T>(string key, string resourceStr) where T : IMessage, new()
        {
            return await ReadMapAsync<T>(Hash.LoadHex(key), resourceStr);
        }
        
        public byte[] ReadMap<T>(Hash keyHash, string resourceStr) where T : IMessage, new()
        {
            return ReadMapAsync<T>(keyHash, resourceStr).Result;
        }
        
        public byte[] ReadMap<T>(IMessage message, string resourceStr) where T : IMessage, new()
        {
            return ReadMapAsync<T>(Hash.FromMessage(message).DumpHex(), resourceStr).Result;
        }

        public byte[] ReadMap<T>(string key, string resourceStr) where T : IMessage, new()
        {
            return ReadMapAsync<T>(Hash.FromString(key), resourceStr).Result;
        }
    }
}