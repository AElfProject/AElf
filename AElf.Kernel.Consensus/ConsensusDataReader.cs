using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel.Managers;
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
        private static Hash ChainId => Hash.LoadBase58(ChainConfig.Instance.ChainId);
        private static Address ContractAddress => ContractHelpers.GetConsensusContractAddress(ChainId);
        
        private readonly IStateManager _stateManager;

        public ConsensusDataReader(IStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        private DataProvider DataProvider
        {
            get
            {
                var dp = DataProvider.GetRootDataProvider(ChainId, ContractAddress);
                dp.StateManager = _stateManager;
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
            return ReadMapAsync<T>(Hash.FromMessage(message).ToHex(), resourceStr).Result;
        }

        public byte[] ReadMap<T>(string key, string resourceStr) where T : IMessage, new()
        {
            return ReadMapAsync<T>(Hash.FromString(key), resourceStr).Result;
        }
    }
}