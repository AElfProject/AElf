using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Blockchains.BasicBaseChain;
using AElf.Contracts.Deployer;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.OS.Node.Application;
using AElf.Types;
using Microsoft.Extensions.Options;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider : IGenesisSmartContractDtoProvider
    {
        private readonly IReadOnlyDictionary<string, byte[]> _codes =
            ContractsDeployer.GetContractCodes<GenesisSmartContractDtoProvider>();
        
        private readonly ConsensusOptions _consensusOptions;
        private readonly TokenInitialOptions _tokenInitialOptions;
        private readonly ContractOptions _contractOptions;

        public GenesisSmartContractDtoProvider(IOptionsProvider optionsProvider)
        {
            _consensusOptions = optionsProvider.ConsensusOptions;
            _tokenInitialOptions = optionsProvider.TokenInitialOptions;
            _contractOptions = optionsProvider.ContractOptions;
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
//                GetGenesisSmartContractDtosForResource(zeroContractAddress),
                GetGenesisSmartContractDtosForCrossChain(zeroContractAddress),
                GetGenesisSmartContractDtosForParliament(),
                GetGenesisSmartContractDtosForConsensus(zeroContractAddress),
            }.SelectMany(x => x);
        }

        public ContractZeroInitializationInput GetContractZeroInitializationInput()
        {
            var contractZeroInitializationInput = new ContractZeroInitializationInput
            {
                ZeroOwnerAddressGenerationContractHashName = ParliamentAuthContractAddressNameProvider.Name,
                ZeroOwnerAddressGenerationMethodName = nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetZeroOwnerAddress),
                ContractDeploymentAuthorityRequired = true
            };
            
            return contractZeroInitializationInput;
        }
    }
}