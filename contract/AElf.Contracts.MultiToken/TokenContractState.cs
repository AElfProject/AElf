using Acs0;
using Acs1;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TokenConverter;
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
        public SingletonState<TokenSymbolList> PreviousBlockTransactionFeeTokenSymbolList { get; set; }

        /// <summary>
        /// symbol -> address -> is in white list.
        /// </summary>
        public MappedState<string, Address, bool> LockWhiteLists { get; set; }

        public MappedState<Hash, CrossChainReceiveTokenInput> VerifiedCrossChainTransferTransaction { get; set; }
        internal CrossChainContractContainer.CrossChainContractReferenceState CrossChainContract { get; set; }

        internal TokenConverterContractContainer.TokenConverterContractReferenceState TokenConverterContract { get; set; }
    }
}