using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Storages;
using NLog;

namespace AElf.Kernel.Managers
{
    public class MinersManager : IMinersManager
    {
        private readonly IDataStore _dataStore;

        private readonly ILogger _logger = LogManager.GetLogger(nameof(MinersManager));

        private static Hash Key => Hash.FromRawBytes(GlobalConfig.AElfDPoSOngoingMinersString.CalculateHash());

        public MinersManager(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public async Task<Miners> GetMiners()
        {
            return await _dataStore.GetAsync<Miners>(Key);
        }

        public async Task SetMiners(Miners miners)
        {
            foreach (var node in miners.Nodes)
            {
                _logger?.Trace($"Set miner {node.DumpHex()} to data store.");
            }

            await _dataStore.InsertAsync(Key, miners);
        }
    }
}