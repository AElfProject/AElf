namespace AElf.Contracts.Consensus.AEDPoS;

// ReSharper disable once InconsistentNaming
public static class AEDPoSContractConstants
{
    public const int MaximumTinyBlocksCount = 1;
    public const int SupposedMinersCount = 17;
    public const int KeepRounds = 40960;
    public const long TolerableMissedTimeSlotsCount = 60 * 24 * 3; // one time slot per minute and last 3 days.
}