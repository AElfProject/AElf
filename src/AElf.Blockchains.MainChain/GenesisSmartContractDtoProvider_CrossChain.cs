using System.Collections.Generic;
using AElf.Contracts.CrossChain;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Consensus.AElfConsensus;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForCrossChain(
            Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract<CrossChainContract>(CrossChainSmartContractAddressNameProvider.Name,
                GenerateCrossChainInitializationCallList());

            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateCrossChainInitializationCallList()
        {
            var crossChainMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            crossChainMethodCallList.Add(nameof(CrossChainContract.Initialize),
                new AElf.Contracts.CrossChain.InitializeInput
                {
                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    ParliamentContractSystemName = ParliamentAuthContractAddressNameProvider.Name
                });
            return crossChainMethodCallList;
        }
    }
}