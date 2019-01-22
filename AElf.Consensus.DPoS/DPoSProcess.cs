namespace AElf.Consensus.DPoS
{
    // ReSharper disable InconsistentNaming
    public enum DPoSProcess
    {
        NoOperationPerformed = 0,
        InitialTerm,
        NextTerm,
        SnapshotForTerm,
        SnapshotForMiners,
        SendDividends,
        NextRound,
        PackageOutValue,
        BroadcastInValue
    }
}