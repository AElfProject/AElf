using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Domain
{
    public class MinersManager : IMinersManager
    {
        private readonly IMinersStore _minersStore;

        public ILogger<MinersManager> Logger { get; set; }

        public MinersManager(IMinersStore minersStore)
        {
            _minersStore = minersStore;
            Logger = NullLogger<MinersManager>.Instance;
        }

        public async Task<Miners> GetMiners(ulong termNumber)
        {
            /*Miners miners;
            if (termNumber != 0)
            {
                miners = await GetMiners(CalculateKey(termNumber));
                if (miners != null && miners.PublicKeys.Any())
                    return miners;
            }

            var dict = MinersConfig.Instance.Producers;
            miners = new Miners();

            foreach (var bp in dict.Values)
            {
                miners.PublicKeys.Add(bp["public_key"]);
            }

            miners.MainchainLatestTermNumber = 0;

            return miners;*/
            throw new NotImplementedException();
        }

        public async Task<bool> IsMinersInDatabase()
        {
            var miners = await GetMiners(CalculateKey(1));
            return miners != null && !miners.IsEmpty();
        }

        public async Task SetMiners(Miners miners, int chainId)
        {
            if (ChainHelpers.ConvertChainIdToBase58(chainId) != GlobalConfig.DefaultChainId)
            {
                return;
            }

            foreach (var publicKey in miners.PublicKeys)
            {
                Logger.LogTrace($"Set miner {publicKey} to data store.");
            }

            if (miners.TermNumber > 1)
            {
                // To inform sidechain latest version of miners list of mainchain.
                Logger.LogTrace($"BP-term for sidechain: {miners.TermNumber}");
                var minersOfTerm1 = await GetMiners(1);
                minersOfTerm1.MainchainLatestTermNumber = miners.TermNumber;
                await SetMiners(CalculateKey(1), minersOfTerm1);
            }

            await SetMiners(CalculateKey(miners.TermNumber), miners);
        }

        private async Task<Miners> GetMiners(string key)
        {
            return await _minersStore.GetAsync<Miners>(key);
        }

        private async Task SetMiners(string key, Miners miners)
        {
            await _minersStore.SetAsync(key, miners);
        }

        private string CalculateKey(ulong termNumber)
        {
            return Hash.FromTwoHashes(Hash.FromRawBytes("AElfMainchainMiners".CalculateHash()),
                Hash.FromMessage(termNumber.ToUInt64Value())).ToHex();
        }
    }
}