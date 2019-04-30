using System;
using System.Collections.Generic;
using System.Linq;
using AElf.OS.Node.Application;
using Microsoft.Extensions.Options;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly DPoSOptions _dposOptions;
        private readonly TokenInitialOptions _tokenInitialOptions;

        public GenesisSmartContractDtoProvider(IOptionsSnapshot<DPoSOptions> dposOptions,
            IOptionsSnapshot<TokenInitialOptions> tokenInitialOptions)
        {
            _dposOptions = dposOptions.Value;
            _tokenInitialOptions = tokenInitialOptions.Value;
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos(Address zeroContractAddress)
        {
            // The order matters !!!
            return new[]
            {
                GetGenesisSmartContractDtosForConsensus(zeroContractAddress),
                GetGenesisSmartContractDtosForDividend(zeroContractAddress),
                GetGenesisSmartContractDtosForToken(zeroContractAddress),
                GetGenesisSmartContractDtosForResource(zeroContractAddress),
                GetGenesisSmartContractDtosForCrossChain(zeroContractAddress),
                GetGenesisSmartContractDtosForVote(zeroContractAddress),
                GetGenesisSmartContractDtosForParliament()
//                GetGenesisSmartContractDtosForElection(zeroContractAddress),
            }.SelectMany(x => x);
        }
    }
}