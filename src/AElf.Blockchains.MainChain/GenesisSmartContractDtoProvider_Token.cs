using System.Collections.Generic;
using System.Linq;
using Acs0;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        private IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForToken()
        {
            var callList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            callList.Add(nameof(TokenContractContainer.TokenContractStub.InitializeAuthorizedController),
                new Empty());
            var l = new List<GenesisSmartContractDto>();
            l.AddGenesisSmartContract(
                GetContractCodeByName("AElf.Contracts.MultiToken"),
                TokenSmartContractAddressNameProvider.Name,
                callList);
            return l;
        }
    }
}