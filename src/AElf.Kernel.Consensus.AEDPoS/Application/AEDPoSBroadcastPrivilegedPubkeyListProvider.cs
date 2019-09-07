using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class AEDPoSBroadcastPrivilegedPubkeyListProvider : IBroadcastPrivilegedPubkeyListProvider
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly IAccountService _accountService;

        public AEDPoSBroadcastPrivilegedPubkeyListProvider(IBlockExtraDataService blockExtraDataService,
            IAccountService accountService)
        {
            _blockExtraDataService = blockExtraDataService;
            _accountService = accountService;
        }

        public async Task<List<string>> GetPubkeyList(BlockHeader blockHeader)
        {
            var consensusExtraData = _blockExtraDataService.GetExtraDataFromBlockHeader("Consensus", blockHeader);
            var information = AElfConsensusHeaderInformation.Parser.ParseFrom(consensusExtraData);
            var round = information.Round;
            var currentPubkey = (await _accountService.GetPublicKeyAsync()).ToHex();
            if (round.RealTimeMinersInformation.Values.Any(m => m.OutValue != null) &&
                round.RealTimeMinersInformation.ContainsKey(currentPubkey))
            {
                // At least someone mined blocks during current round.
                var currentOrder = round.RealTimeMinersInformation.Values.First(m => m.Pubkey == currentPubkey).Order;
                var minersCount = round.RealTimeMinersInformation.Count;
                var ebp = round.RealTimeMinersInformation.Values.First(m => m.IsExtraBlockProducer).Pubkey;
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