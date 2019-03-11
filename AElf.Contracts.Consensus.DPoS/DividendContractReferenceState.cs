using System;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.DPoS
{
    public class DividendContractReferenceState : ContractReferenceState
    {
        public Action<ulong> KeepWeights { get; set; }
        public Action<ulong, ulong> SubWeights { get; set; }
        public Action<ulong, ulong> AddWeights { get; set; }
        public Action<ulong, ulong> AddDividends { get; set; }
        public Action<VotingRecord> TransferDividends { get; set; }
        public Action<Address, ulong> SendDividends { get; set; }
    }
}