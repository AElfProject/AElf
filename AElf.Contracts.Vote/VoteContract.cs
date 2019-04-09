using System;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Vote
{
    public partial class VoteContract : VoteContractContainer.VoteContractBase
    {
        public override Empty Register(VotingRegisterInput input)
        {
            return base.Register(input);
        }
    }
}