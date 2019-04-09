using System;
using System.Threading.Tasks;
using AElf.Consensus.DPoS;
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

        public async Task<MinerList> GetCurrentMiners(ChainContext chainContext)
        {
            return await _consensusInformationGenerationService.ExecuteContractAsync<MinerList>(chainContext,
                "GetCurrentMiners", new Empty(), DateTime.UtcNow);
        }
    }
}