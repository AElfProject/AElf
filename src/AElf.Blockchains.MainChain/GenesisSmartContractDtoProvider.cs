using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.OS.Node.Application;
using Microsoft.Extensions.Options;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly ConsensusOptions _consensusOptions;
        private readonly TokenInitialOptions _tokenInitialOptions;
        private readonly ContractOptions _contractOptions;

        public GenesisSmartContractDtoProvider(IOptionsSnapshot<ConsensusOptions> dposOptions,
            IOptionsSnapshot<ContractOptions> contractOptions, IOptionsSnapshot<TokenInitialOptions> tokenInitialOptions)
        {
            _consensusOptions = dposOptions.Value;
            _tokenInitialOptions = tokenInitialOptions.Value;
            _contractOptions = contractOptions.Value;
        }

        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtos(Address zeroContractAddress)
        {
            // The order matters !!!
            return new[]
            {
                GetGenesisSmartContractDtosForVote(zeroContractAddress),
                GetGenesisSmartContractDtosForProfit(zeroContractAddress),
                GetGenesisSmartContractDtosForElection(zeroContractAddress),
                GetGenesisSmartContractDtosForToken(zeroContractAddress),
                GetGenesisSmartContractDtosForResource(zeroContractAddress),
                GetGenesisSmartContractDtosForCrossChain(zeroContractAddress),
                GetGenesisSmartContractDtosForParliament(),
                GetGenesisSmartContractDtosForConsensus(zeroContractAddress),
            }.SelectMany(x => x);
        }

        public ContractZeroInitializationInput GetContractZeroInitializationInput()
        {
            var contractZeroInitializationInput = new ContractZeroInitializationInput();
            if (!_contractOptions.IsContractDeploymentAllowed)
            {
                contractZeroInitializationInput.ParliamentAuthContractName =
                    ParliamentAuthContractAddressNameProvider.Name;
            }
            
            return contractZeroInitializationInput;
        }
    }
}