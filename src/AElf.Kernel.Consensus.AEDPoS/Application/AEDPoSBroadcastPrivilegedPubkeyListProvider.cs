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
    // ReSharper disable once InconsistentNaming
    public class AEDPoSBroadcastPrivilegedPubkeyListProvider : IBroadcastPrivilegedPubkeyListProvider
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly IAccountService _accountService;
        private readonly IConsensusExtraDataProvider _consensusExtraDataProvider;

        public ILogger<AEDPoSBroadcastPrivilegedPubkeyListProvider> Logger { get; set; }

        public AEDPoSBroadcastPrivilegedPubkeyListProvider(IBlockExtraDataService blockExtraDataService,
            IAccountService accountService, IConsensusExtraDataProvider consensusExtraDataProvider)
        {
            _blockExtraDataService = blockExtraDataService;
            _accountService = accountService;
            _consensusExtraDataProvider = consensusExtraDataProvider;

            Logger = NullLogger<AEDPoSBroadcastPrivilegedPubkeyListProvider>.Instance;
        }

        public async Task<List<string>> GetPubkeyList(BlockHeader blockHeader)
        {
            var consensusExtraData =
                _blockExtraDataService.GetExtraDataFromBlockHeader(_consensusExtraDataProvider.BlockHeaderExtraDataKey,
                    blockHeader);
            if (consensusExtraData == null) return new List<string>();
            var consensusInformation = AElfConsensusHeaderInformation.Parser.ParseFrom(consensusExtraData);
            if (consensusInformation.Behaviour == AElfConsensusBehaviour.TinyBlock)
            {
                // The orders changed every round, and the orders can be updated during every behaviour of UPDATE_VALUE or NEXT_ROUND,
                // so we can skip the update for TINY_BLOCK.
                return new List<string>();
            }

            var round = consensusInformation.Round;
            var currentPubkey = (await _accountService.GetPublicKeyAsync()).ToHex();
            var minersCount = round.RealTimeMinersInformation.Count;
            if (round.RealTimeMinersInformation.Values.Any(m => m.OutValue != null) &&
                round.RealTimeMinersInformation.ContainsKey(currentPubkey))
            {
                // If any miner mined blocks during current round.
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