using System.Collections.Generic;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel;
using AElf.Kernel.Consensus.AElfConsensus;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForParliament()
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract<ParliamentAuthContract>(ParliamentAuthContractAddressNameProvider.Name,
                GenerateParliamentInitializationCallList());

            return l;
        }
        
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateParliamentInitializationCallList()
        {
            var parliamentInitializationCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            parliamentInitializationCallList.Add(nameof(ParliamentAuthContract.Initialize),
                new ParliamentAuthInitializationInput
                {
                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name
                });
            return parliamentInitializationCallList;
        }
    }
}