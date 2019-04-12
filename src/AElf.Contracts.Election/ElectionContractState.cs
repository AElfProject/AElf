using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractState : ContractState
    {
        public BoolState Initialized { get; set; }

        public MappedState<string, bool?> Candidates { get; set; }

        public MappedState<string, Votes> Votes { get; set; }

        public MappedState<string, CandidateHistory> Histories { get; set; }

        public SingletonState<int> CurrentTermNumber { get; set; }

    }
}