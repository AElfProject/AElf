using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.MultiToken;

public partial class TokenContractState : ContractState
{
    public StringState NativeTokenSymbol { get; set; }

    public StringState ChainPrimaryTokenSymbol { get; set; }
    public MappedState<string, TokenInfo> TokenInfos { get; set; }
    public MappedState<string, string> SymbolSeedMap { get; set; }
    public MappedState<Address, string, long> Balances { get; set; }
    public MappedState<Address, Address, string, long> Allowances { get; set; }

    public SingletonState<Address> FeeReceiver { get; set; }

    /// <summary>
    ///     Contract Address -> Advance Address -> Resource Token Symbol -> Amount.
    /// </summary>
    public MappedState<Address, Address, string, long> AdvancedResourceToken { get; set; }

    /// <summary>
    ///     Contract Address -> (Owning) Resource Token Symbol -> Amount.
    /// </summary>
    public MappedState<Address, string, long> OwningResourceToken { get; set; }

    public BoolState InitializedFromParentChain { get; set; }

    public SingletonState<AuthorityInfo> CrossChainTokenContractRegistrationController { get; set; }
    public SingletonState<UserFeeController> UserFeeController { get; set; }
    public SingletonState<DeveloperFeeController> DeveloperFeeController { get; set; }
    public SingletonState<AuthorityInfo> SymbolToPayTxFeeController { get; set; }
    public SingletonState<AuthorityInfo> SideChainRentalController { get; set; }

    /// <summary>
    ///     symbol -> address -> is in white list.
    /// </summary>
    public MappedState<string, Address, bool> LockWhiteLists { get; set; }

    public MappedState<int, Address> CrossChainTransferWhiteList { get; set; }

    public MappedState<Hash, bool> VerifiedCrossChainTransferTransaction { get; set; }

    public SingletonState<AuthorityInfo> MethodFeeController { get; set; }

    public SingletonState<Hash> LatestTotalResourceTokensMapsHash { get; set; }
    public SingletonState<Hash> LatestTotalTransactionFeesMapHash { get; set; }

    public SingletonState<long> ClaimTransactionFeeExecuteHeight { get; set; }
    public SingletonState<long> DonateResourceTokenExecuteHeight { get; set; }

    public SingletonState<Address> NFTContractAddress { get; set; }

    public MappedState<Address, bool> CreateTokenWhiteListMap { get; set; }

    public MappedState<Address, TransactionFeeDelegatees> TransactionFeeDelegateesMap { get; set; }
    
    /// <summary>
    /// delegator address -> contract address -> method name -> delegatee info
    /// </summary>
    public MappedState<Address, Address, string, TransactionFeeDelegatees> TransactionFeeDelegateInfoMap { get; set; }
}