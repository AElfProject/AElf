using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Consensus.AElfConsensus;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.AElfConsensus.Application
{
    public class AElfConsensusInformationProvider : IAElfConsensusInformationProvider
    {
        private readonly IConsensusInformationGenerationService _consensusInformationGenerationService;

        public AElfConsensusInformationProvider(IConsensusInformationGenerationService consensusInformationGenerationService)
        {
            _consensusInformationGenerationService = consensusInformationGenerationService;
        }

        public async Task<IEnumerable<string>> GetCurrentMiners(ChainContext chainContext)
        {
            var minersWithRoundNumber = await _consensusInformationGenerationService.ExecuteContractAsync<MinerListWithRoundNumber>(chainContext,
                "GetCurrentMiners", new Empty(), DateTime.UtcNow);
            return minersWithRoundNumber.MinerList.PublicKeys.Select(k => k.ToHex());
        }
    }
}