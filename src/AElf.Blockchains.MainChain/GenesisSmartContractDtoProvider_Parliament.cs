using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel;
using AElf.OS.Node.Application;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForParliament()
        {
            var l = new List<GenesisSmartContractDto>();
//            l.AddGenesisSmartContract<ParliamentAuthContract>(
            l.AddGenesisSmartContract(
                _codes.Single(kv=>kv.Key.Contains("ParliamentAuth")).Value,
                ParliamentAuthSmartContractAddressNameProvider.Name,
                GenerateParliamentInitializationCallList());

            return l;
        }
        
        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateParliamentInitializationCallList()
        {
            var parliamentInitializationCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            parliamentInitializationCallList.Add(
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Initialize),
                new Contracts.ParliamentAuth.InitializeInput
                {
                    GenesisOwnerReleaseThreshold = _contractOptions.GenesisOwnerReleaseThreshold
                });
            return parliamentInitializationCallList;
        }
    }
}