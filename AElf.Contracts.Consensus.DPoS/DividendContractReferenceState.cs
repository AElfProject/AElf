using System;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public class DividendContractReferenceState : ContractReferenceState
    {
        public Action<long> KeepWeights { get; set; }
        public Action<long, long> SubWeights { get; set; }
        public Action<long, long> AddWeights { get; set; }
        public Action<long, long> AddDividends { get; set; }
        public Action<VotingRecord> TransferDividends { get; set; }
        public Action<Address, long> SendDividends { get; set; }
    }
}