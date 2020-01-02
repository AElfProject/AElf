using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.Referendum;
using AElf.Kernel;
using AElf.OS.Node.Application;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForReferendum()
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                _codes.Single(kv => kv.Key.Contains("Referendum")).Value,
                ReferendumSmartContractAddressNameProvider.Name,
                GenerateReferendumInitializationCallList());
            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateReferendumInitializationCallList()
        {
            var referendumInitializationCallList =
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            referendumInitializationCallList.Add(
                nameof(ReferendumContractContainer.ReferendumContractStub.Initialize),
                new Empty());
            return referendumInitializationCallList;
        }
    }
}