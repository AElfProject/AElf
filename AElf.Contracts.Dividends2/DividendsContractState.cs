using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using AElf.Sdk.CSharp2.State;

namespace AElf.Contracts.Dividends2
{
    public class ConsensusContractReferenceState : ContractReferenceState
    {
        public Func<ulong> GetCurrentRoundNumber { get; set; }
        public Func<ulong> GetCurrentTermNumber { get; set; }
        public Func<ulong, Round> GetRoundInfo { get; set; }
        public Func<string, Tickets> GetTicketsInfo { get; set; }
        public Func<ulong> GetBlockchainAge { get; set; }
    }

    public class TokenContractReferenceState : ContractReferenceState
    {
        public Action<Address, ulong> Transfer { get; set; }
    }

    public class DividendsContractState : ContractState
    {
        public BoolState Initialized { get; set; }
        public ConsensusContractReferenceState ConsensusContract { get; set; }
        public TokenContractReferenceState TokenContract { get; set; }

        // Term Number -> Dividends Amount
        public MappedState<ulong, ulong> DividendsMap { get; set; }

        // Term Number -> Total weights
        public MappedState<ulong, ulong> TotalWeightsMap { get; set; }

        // Because voter can request dividends of each VotingRecord instance for terms it experienced,
        // we need to record the term number of last term he request his dividends.
        // Hash (of VotingRecord) -> Latest request dividends term number
        public MappedState<Hash, ulong> LastRequestDividendsMap { get; set; }
    }
}