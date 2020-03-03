using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.CrossChain;
using AElf.CrossChain;
using AElf.OS.Node.Application;
using InitializeInput = AElf.Contracts.CrossChain.InitializeInput;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForCrossChain()
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                GetContractCodeByName("AElf.Contracts.CrossChain"),
                CrossChainSmartContractAddressNameProvider.Name,
                GenerateCrossChainInitializationCallList());

            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateCrossChainInitializationCallList()
        {
            var crossChainMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            crossChainMethodCallList.Add(nameof(CrossChainContractContainer.CrossChainContractStub.Initialize),
                new InitializeInput
                {
                    IsPrivilegePreserved = true
                });
            return crossChainMethodCallList;
        }
    }
}