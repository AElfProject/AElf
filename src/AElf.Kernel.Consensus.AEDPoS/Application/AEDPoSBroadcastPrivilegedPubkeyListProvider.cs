using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class AEDPoSBroadcastPrivilegedPubkeyListProvider : IBroadcastPrivilegedPubkeyListProvider
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly IAccountService _accountService;

        private readonly List<string> _cachedPubkeyList = new List<string>();

        public ILogger<AEDPoSBroadcastPrivilegedPubkeyListProvider> Logger { get; set; }

        public AEDPoSBroadcastPrivilegedPubkeyListProvider(IBlockExtraDataService blockExtraDataService,
            IAccountService accountService)
        {
            _blockExtraDataService = blockExtraDataService;
            _accountService = accountService;

            Logger = NullLogger<AEDPoSBroadcastPrivilegedPubkeyListProvider>.Instance;
        }

        public async Task<List<string>> GetPubkeyList(BlockHeader blockHeader)
        {
            var consensusExtraData = _blockExtraDataService.GetExtraDataFromBlockHeader("Consensus", blockHeader);
            var information = AElfConsensusHeaderInformation.Parser.ParseFrom(consensusExtraData);
            if (information.Behaviour == AElfConsensusBehaviour.TinyBlock)
            {
                return _cachedPubkeyList;
            }

            var round = information.Round;
            var currentPubkey = (await _accountService.GetPublicKeyAsync()).ToHex();
            var minersCount = round.RealTimeMinersInformation.Count;
            if (round.RealTimeMinersInformation.Values.Any(m => m.OutValue != null) &&
                round.RealTimeMinersInformation.ContainsKey(currentPubkey))
            {
                // At least someone mined blocks during current round.
                var currentMiner =
                    round.RealTimeMinersInformation.Values.Single(m => m.Pubkey == currentPubkey);
                var currentOrder = currentMiner.Order;
                var ebp = round.RealTimeMinersInformation.Values.Single(m => m.IsExtraBlockProducer).Pubkey;
                if (currentOrder >= minersCount) return new List<string> {ebp};
                var nextMiners = round.RealTimeMinersInformation.Values.Where(m => m.Order > currentOrder)
                    .OrderBy(m => m.Order).Select(m => m.Pubkey).ToList();
                nextMiners.AddIfNotContains(ebp);
                return nextMiners;
            }

            return round.RealTimeMinersInformation.Values.OrderBy(m => m.Order).Select(m => m.Pubkey).ToList();
        }
    }
}