using AElf.Standards.ACS1;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MultiToken;

public partial class TokenContractState
{
    internal MappedState<string, MethodFees> TransactionFees { get; set; }

    public SingletonState<Timestamp> LastPayRentTime { get; set; }

    public SingletonState<Address> SideChainCreator { get; set; }

    public SingletonState<AllCalculateFeeCoefficients> AllCalculateFeeCoefficients { get; set; }
    public SingletonState<SymbolListToPayTxSizeFee> SymbolListToPayTxSizeFee { get; set; }

    /// <summary>
    /// Symbol -> Amount (TBD)
    /// (CPU: core)
    /// (RAM: GiB)
    /// (DISK: GiB)
    /// (NET: MB)
    /// </summary>
    public MappedState<string, int> ResourceAmount { get; set; }

    /// <summary>
    /// Symbol -> Amount (Tokens per minute)
    /// </summary>
    public MappedState<string, long> Rental { get; set; }

    /// <summary>
    /// Symbol -> Amount
    /// </summary>
    public MappedState<string, long> OwningRental { get; set; }
    
    public SingletonState<MethodFeeFreeAllowancesConfig> MethodFeeFreeAllowancesConfig { get; set; }
    public MappedState<Address, MethodFeeFreeAllowances> MethodFeeFreeAllowancesMap { get; set; }
    public MappedState<Address, Timestamp> MethodFeeFreeAllowancesLastRefreshTimeMap { get; set; }
    
    /// <summary>
    ///  Symbol List
    /// </summary>
    public SingletonState<TransactionFeeFreeAllowancesSymbolList> TransactionFeeFreeAllowancesSymbolList { get; set; }
    
    /// <summary>
    /// Symbol -> TransactionFeeFreeAllowanceConfig
    /// </summary>
    public MappedState<string, TransactionFeeFreeAllowanceConfig> TransactionFeeFreeAllowancesConfigMap { get; set; }
    
    /// <summary>
    /// Address -> Symbol -> TransactionFeeFreeAllowanceMap
    /// </summary>
    public MappedState<Address, string, TransactionFeeFreeAllowanceMap> TransactionFeeFreeAllowances { get; set; }
    
    /// <summary>
    /// Address -> Symbol -> LastRefreshTime
    /// </summary>
    public MappedState<Address, string, Timestamp> TransactionFeeFreeAllowancesLastRefreshTimes { get; set; }
}