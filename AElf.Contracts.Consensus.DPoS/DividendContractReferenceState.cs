using System;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.Dividend;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    public class DividendContractReferenceState : ContractReferenceState
    {
        internal MethodReference<SInt64Value, ActionResult> KeepWeights { get; set; }
        internal MethodReference<WeightsInfo, ActionResult> SubWeights { get; set; }
        internal MethodReference<WeightsInfo, SInt64Value> AddWeights { get; set; }
        internal MethodReference<AddDividendsInput, SInt64Value> AddDividends { get; set; }
        internal MethodReference<VotingRecord, SInt64Value> TransferDividends { get; set; }
        internal MethodReference<SendDividendsInput, Empty> SendDividends { get; set; }
    }
}