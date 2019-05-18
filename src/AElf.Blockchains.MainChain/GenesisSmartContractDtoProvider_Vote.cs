using System.Collections.Generic;
using AElf.Contracts.Vote;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Blockchains.MainChain
{
    public partial class GenesisSmartContractDtoProvider
    {
        public IEnumerable<GenesisSmartContractDto> GetGenesisSmartContractDtosForVote(Address zeroContractAddress)
        {
            var l = new List<GenesisSmartContractDto>();

            l.AddGenesisSmartContract<VoteContract>(
                VoteSmartContractAddressNameProvider.Name, GenerateVoteInitializationCallList());

            return l;
        }

        private SystemContractDeploymentInput.Types.SystemTransactionMethodCallList GenerateVoteInitializationCallList()
        {
            var voteContractMethodCallList = new SystemContractDeploymentInput.Types.SystemTransactionMethodCallList();
            voteContractMethodCallList.Add(nameof(VoteContract.InitialVoteContract),new Empty());
            return voteContractMethodCallList;
        }
    }
}