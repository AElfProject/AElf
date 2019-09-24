using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.ReferendumAuth;
using AElf.Kernel;
using AElf.OS.Node.Application;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForReferendum()
        {
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                _codes.Single(kv => kv.Key.Contains("ReferendumAuth")).Value,
                ReferendumAuthSmartContractAddressNameProvider.Name,
                GenerateReferendumfInitializationCallList());
            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList
            GenerateReferendumfInitializationCallList()
        {
            var referendumfInitializationCallList =
                new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            referendumfInitializationCallList.Add(
                nameof(ReferendumAuthContractContainer.ReferendumAuthContractStub.Initialize),
                new Empty());
            return referendumfInitializationCallList;
        }
    }
}