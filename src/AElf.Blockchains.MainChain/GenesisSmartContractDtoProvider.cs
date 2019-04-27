using System.Collections.Generic;
using System.Linq;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly DPoSOptions _dposOptions;
        private readonly TokenInitialOptions _tokenInitialOptions;

        public GenesisSmartContractDtoProvider(DPoSOptions dposOptions,
            TokenInitialOptions tokenInitialOptions)
        {
            _dposOptions = dposOptions;
            _tokenInitialOptions = tokenInitialOptions;
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos(Address zeroContractAddress)
        {
            return new[]
            {
                GetGenesisSmartContractDtosForToken(zeroContractAddress),
                GetGenesisSmartContractDtosForConsensus(zeroContractAddress),
                GetGenesisSmartContractDtosForDividend(zeroContractAddress),
                GetGenesisSmartContractDtosForVote(zeroContractAddress),
//                GetGenesisSmartContractDtosForElection(zeroContractAddress),
                GetGenesisSmartContractDtosForResource(zeroContractAddress),
                GetGenesisSmartContractDtosForCrossChain(zeroContractAddress)
            }.SelectMany(x => x);
        }
    }
}