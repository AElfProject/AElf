namespace AElf.Contracts.Consensus.AEDPoS;

// ReSharper disable once InconsistentNaming
public static class AEDPoSContractConstants
{
    public const int MaximumTinyBlocksCount = 8;
    public const long InitialMiningRewardPerBlock = 12500000;
    public const long TimeToReduceMiningRewardByHalf = 126144000; // 60 * 60 * 24 * 365 * 4
    public const int SupposedMinersCount = 17;
    public const int KeepRounds = 40960;
    public const long TolerableMissedTimeSlotsCount = 60 * 24 * 3; // one time slot per minute and last 3 days.
    public const string SideChainShareProfitsTokenSymbol = "SHARE";
    public const string PayTxFeeSymbolListName = "SymbolListToPayTxFee";
    public const string PayRentalSymbolListName = "SymbolListToPayRental";
    public const string SecretSharingEnabledConfigurationKey = "SecretSharingEnabled";
}