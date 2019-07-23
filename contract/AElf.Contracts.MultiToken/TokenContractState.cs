using Acs1;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.MultiToken
{
    public class TokenContractState : ContractState
    {
        public StringState NativeTokenSymbol { get; set; }
        public MappedState<string, TokenAmount> MethodFees { get; set; }
        public MappedState<string, TokenInfo> TokenInfos { get; set; }
        public MappedState<Address, string, long> Balances { get; set; }
        public MappedState<Address, Address, string, long> Allowances { get; set; }
        public MappedState<Address, string, long> ChargedFees { get; set; }
        public SingletonState<Address> FeePoolAddress { get; set; }
        
        public SingletonState<bool> Initialized { get; set; }

        public SingletonState<Address> Owner { get; set; }
        
        public SingletonState<bool> IsMainChain { get; set; }
        
        public SingletonState<Address> MainChainTokenContractAddress { get; set; }
        
        /// <summary>
        /// symbol -> address -> is in white list.
        /// </summary>
        public MappedState<string, Address, bool> LockWhiteLists { get; set; }
        
        public MappedState<int, Address> CrossChainTransferWhiteList { get; set; }

        public MappedState<Hash, CrossChainReceiveTokenInput> VerifiedCrossChainTransferTransaction { get; set; }
        internal CrossChainContractContainer.CrossChainContractReferenceState CrossChainContractReferenceState { get; set; }
    }
}