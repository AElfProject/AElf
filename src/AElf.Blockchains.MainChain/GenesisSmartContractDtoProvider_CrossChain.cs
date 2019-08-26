using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.CrossChain;
using AElf.CrossChain;
using AElf.OS.Node.Application;
using AElf.Types;
using InitializeInput = AElf.Contracts.CrossChain.InitializeInput;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForCrossChain(
            Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("CrossChain")).Value,
                CrossChainSmartContractAddressNameProvider.Name,
                GenerateCrossChainInitializationCallList());

            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateCrossChainInitializationCallList()
        {
            var crossChainMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            crossChainMethodCallList.Add(nameof(CrossChainContractContainer.CrossChainContractStub.Initialize),
                new InitializeInput());
            return crossChainMethodCallList;
        }
    }
}