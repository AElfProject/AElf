using Acs0;
using Acs1;
using AElf.Contracts.Association;
using AElf.Contracts.Parliament;
using AElf.Contracts.Referendum;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenHolder;
using AElf.Contracts.Treasury;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.MultiToken
{
    public partial class TokenContractState : ContractState
    {
        public StringState NativeTokenSymbol { get; set; }

        public StringState ChainPrimaryTokenSymbol { get; set; }
        public MappedState<string, TokenInfo> TokenInfos { get; set; }
        public MappedState<Address, string, long> Balances { get; set; }
        public MappedState<Address, Address, string, long> Allowances { get; set; }

        public SingletonState<Address> FeeReceiver { get; set; }

        /// <summary>
        /// Contract Address -> Advance Address -> Resource Token Symbol -> Amount.
        /// </summary>
        public MappedState<Address, Address, string, long> AdvancedResourceToken { get; set; }

        /// <summary>
        /// Contract Address -> (Owning) Resource Token Symbol -> Amount.
        /// </summary>
        public MappedState<Address, string, long> OwningResourceToken { get; set; }

        public BoolState InitializedFromParentChain { get; set; }

        public MappedState<Address, ProfitReceivingInformation> ProfitReceivingInfos { get; set; }
        public SingletonState<AuthorityInfo> CrossChainTokenContractRegistrationController { get; set; }
        public SingletonState<UserFeeController> UserFeeController { get; set; }
        public SingletonState<DeveloperFeeController> DeveloperFeeController { get; set; }
        public SingletonState<AuthorityInfo> SymbolToPayTxFeeController { get; set; }
        public SingletonState<AuthorityInfo> SideRentalParliamentController { get; set; }

        /// <summary>
        /// symbol -> address -> is in white list.
        /// </summary>
        public MappedState<string, Address, bool> LockWhiteLists { get; set; }

        public MappedState<int, Address> CrossChainTransferWhiteList { get; set; }

        public MappedState<Hash, bool> VerifiedCrossChainTransferTransaction { get; set; }
        internal TreasuryContractContainer.TreasuryContractReferenceState TreasuryContract { get; set; }

        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }

        internal ACS0Container.ACS0ReferenceState ZeroContract { get; set; }
        internal AssociationContractContainer.AssociationContractReferenceState AssociationContract { get; set; }
        internal ReferendumContractContainer.ReferendumContractReferenceState ReferendumContract { get; set; }
        internal TokenHolderContractContainer.TokenHolderContractReferenceState TokenHolderContract { get; set; }
        internal ProfitContractContainer.ProfitContractReferenceState ProfitContract { get; set; }

        public SingletonState<AuthorityInfo> MethodFeeController { get; set; }

        public Int32State MinimumProfitsDonationPartsPerHundred { get; set; }

        public SingletonState<Hash> LatestTotalResourceTokensMapsHash { get; set; }
        public SingletonState<Hash> LatestTotalTransactionFeesMapHash { get; set; }
    }
}