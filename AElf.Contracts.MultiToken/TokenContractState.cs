using System;
using AElf.Common;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.MultiToken
{
    public class TokenContractState : ContractState
    {
        public StringState NativeTokenSymbol { get; set; }
        public MappedState<string, long> MethodFees { get; set; }
        public MappedState<string, TokenInfo> TokenInfos { get; set; }
        public MappedState<Address, string, long> Balances { get; set; }
        public MappedState<Address, Address, string, long> Allowances { get; set; }
        public MappedState<Address, string, long> ChargedFees { get; set; }
        public SingletonState<Address> FeePoolAddress { get; set; }
        /// <summary>
        /// symbol -> address -> is in white list.
        /// </summary>
        public MappedState<string, Address, bool> LockWhiteLists { get; set; }

        public MappedState<Hash, CrossChainReceiveInput> VerifiedCrossChainTransferTransaction { get; set; }
        public CrossChainContractReferenceState CrossChainContractReferenceState { get; set; }
    }
    
    public class CrossChainContractReferenceState : ContractReferenceState
    {
        public Func<Hash, MerklePath, long, bool> VerifyTransaction { get; set; }
    }
    
}