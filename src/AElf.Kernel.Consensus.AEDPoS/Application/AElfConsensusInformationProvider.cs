using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class IaedpoSInformationProvider : IAEDPoSInformationProvider
    {
        private readonly IConsensusInformationGenerationService _consensusInformationGenerationService;

        public IaedpoSInformationProvider(IConsensusInformationGenerationService consensusInformationGenerationService)
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