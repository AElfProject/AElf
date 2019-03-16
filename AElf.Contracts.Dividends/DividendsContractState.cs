using System;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Dividends
{
    public class ConsensusContractReferenceState : ContractReferenceState
    {
        public Func<long> GetCurrentRoundNumber { get; set; }
        public Func<long> GetCurrentTermNumber { get; set; }
        public Func<long, Round> GetRoundInfo { get; set; }
        public Func<string, Tickets> GetTicketsInfo { get; set; }
        public Func<long> GetBlockchainAge { get; set; }
    }

    public class TokenContractReferenceState : ContractReferenceState
    {
        public Action<TransferInput> Transfer { get; set; }
    }

    public class DividendsContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public ConsensusContractReferenceState ConsensusContract { get; set; }
        public TokenContractReferenceState TokenContract { get; set; }

        // Term Number -> Dividends Amount
        public MappedState<long, long> DividendsMap { get; set; }

        // Term Number -> Total weights
        public MappedState<long, long> TotalWeightsMap { get; set; }

        // Because voter can request dividends of each VotingRecord instance for terms it experienced,
        // we need to record the term number of last term he request his dividends.
        // Hash (of VotingRecord) -> Latest request dividends term number
        public MappedState<Hash, long> LastRequestedDividendsMap { get; set; }

        public SingletonState<string> StarterPublicKey { get; set; }
    }
}