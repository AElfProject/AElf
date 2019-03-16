using System;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Consensus.DPoS
{
    public class TokenContractReferenceState : ContractReferenceState
    {
        public Action<TransferFromInput> TransferFrom { get; set; }
        public Action<TransferInput> Transfer { get; set; }
    }
}