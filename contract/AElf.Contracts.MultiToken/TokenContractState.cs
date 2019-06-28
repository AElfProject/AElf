using Acs0;
using Acs1;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
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

        /// <summary>
        /// Contract Address -> Resource Token Symbol -> Amount.
        /// </summary>
        public MappedState<Address, string, long> ChangedResources { get; set; }

        public MappedState<Address, ProfitReceivingInformation> ProfitReceivingInfos { get; set; }
        public SingletonState<TokenSymbolList> PreviousBlockTransactionFeeTokenSymbolList { get; set; }

        /// <summary>
        /// symbol -> address -> is in white list.
        /// </summary>
        public MappedState<string, Address, bool> LockWhiteLists { get; set; }

        public MappedState<Hash, CrossChainReceiveTokenInput> VerifiedCrossChainTransferTransaction { get; set; }
        internal CrossChainContractContainer.CrossChainContractReferenceState CrossChainContract { get; set; }

        internal TreasuryContractContainer.TreasuryContractReferenceState TreasuryContract { get; set; }

        internal TokenConverterContractContainer.TokenConverterContractReferenceState TokenConverterContract
        {
            get;
            set;
        }

        internal ACS0Container.ACS0ReferenceState ACS0Contract { get; set; }

        public SingletonState<long> CpuUnitPrice { get; set; }
        public SingletonState<long> StoUnitPrice { get; set; }
        public SingletonState<long> NetUnitPrice { get; set; }
    }
}