using System;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Dividend
{
    public class ConsensusContractReferenceState : ContractReferenceState
    {
        internal MethodReference<Empty,SInt64Value> GetCurrentTermNumber { get; set; }
        internal MethodReference<SInt64Value, Round> GetRoundInformation { get; set; }
        internal MethodReference<PublicKey, Tickets> GetTicketsInformation { get; set; }
        internal MethodReference<Empty,SInt64Value> GetBlockchainAge { get; set; }
    }

    public class TokenContractReferenceState : ContractReferenceState
    {
        public MethodReference<TransferInput, Empty> Transfer { get; set; }
    }

    public class DividendsContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public ConsensusContractReferenceState ConsensusContract { get; set; }
        public TokenContractReferenceState TokenContract { get; set; }
        public BasicContractZeroReferenceState BasicContractZero { get; set; }

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