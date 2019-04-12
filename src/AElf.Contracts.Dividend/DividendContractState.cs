using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Dividend
{
    public class DividendsContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        internal ConsensusContractContainer.ConsensusContractReferenceState ConsensusContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal  BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }

        // Term Number -> Dividends Amount
        public MappedState<long, long> DividendsMap { get; set; }

        // Term Number -> Total weights
        public MappedState<long, long> TotalWeightsMap { get; set; }

        // Because voter can request dividends of each VotingRecord instance for terms it experienced,
        // we need to record the term number of last term he request his dividends.
        // Hash (of VotingRecord) -> Latest request dividends term number
        public MappedState<Hash, long> LastRequestedDividendsMap { get; set; }

        public SingletonState<string> StarterPublicKey { get; set; }

        public SingletonState<Hash> ConsensusContractSystemName { get; set; }
        public SingletonState<Hash> TokenContractSystemName { get; set; }
    }
}