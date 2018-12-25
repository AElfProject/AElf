using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
using AElf.Kernel.Storages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Managers
{
    public class MinersManager : IMinersManager
    {
        private readonly IMinersStore _minersStore;

        public ILogger<MinersManager> Logger { get; set; }
        private static Hash Key => Hash.FromRawBytes(GlobalConfig.AElfDPoSMinersString.CalculateHash());

        public MinersManager(IMinersStore minersStore)
        {
            _minersStore = minersStore;
            Logger = NullLogger<MinersManager>.Instance;
        }

        public async Task<Miners> GetMiners()
        {
            var miners = await GetMiners(Key.ToHex());
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
            var miners = await GetMiners(Key.ToHex());
            return miners != null && !miners.IsEmpty();
        }

        public async Task SetMiners(Miners miners)
        {
            foreach (var publicKey in miners.PublicKeys)
            {
                Logger.LogTrace($"Set miner {publicKey} to data store.");
            }

            await SetMiners(Key.ToHex(), miners);
        }

        private async Task<Miners> GetMiners(string key)
        {
            return await _minersStore.GetAsync<Miners>(key);
        }

        private async Task SetMiners(string key, Miners miners)
        {
            await _minersStore.SetAsync(key, miners);
        }
    }
}