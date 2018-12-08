using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
using AElf.Kernel.Storages;
using Google.Protobuf;
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
            var miners = await _dataStore.GetAsync<Miners>(Key);
            if (miners != null && !miners.IsEmpty())
                return miners;

            var dict = MinersConfig.Instance.Producers;
            miners = new Miners();

            foreach (var bp in dict.Values)
            {
                miners.PublicKeys.Add(bp["public_key"]);
            }

            return miners;
        }

        public async Task<bool> IsMinersInDatabase()
        {
            var miners = await _dataStore.GetAsync<Miners>(Key);
            return miners != null && !miners.IsEmpty();
        }

        public async Task SetMiners(Miners miners)
        {
            foreach (var publicKey in miners.PublicKeys)
            {
                _logger?.Trace($"Set miner {publicKey} to data store.");
            }

            await _dataStore.InsertAsync(Key, miners);
        }
    }
}