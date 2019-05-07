using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acs4;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.DPoS
{
    public class DPoSInformationProvider : IDPoSInformationProvider
    {
        private readonly IConsensusInformationGenerationService _consensusInformationGenerationService;

        public DPoSInformationProvider(IConsensusInformationGenerationService consensusInformationGenerationService)
        {
            _consensusInformationGenerationService = consensusInformationGenerationService;
        }

        public async Task<IEnumerable<string>> GetCurrentMiners(ChainContext chainContext)
        {
            var minersWithRoundNumber = await _consensusInformationGenerationService.ExecuteContractAsync<MinerListWithRoundNumber>(chainContext,
                "GetCurrentMiners", new Empty(), DateTime.UtcNow);
            return minersWithRoundNumber.MinerList.PublicKeys;
        }
    }
}