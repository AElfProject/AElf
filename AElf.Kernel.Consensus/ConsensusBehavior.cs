namespace AElf.Kernel.Consensus
{
    // ReSharper disable once InconsistentNaming
    public enum ConsensusBehavior
    {
        DoNothing = 0,
        // ReSharper disable once InconsistentNaming
        InitializeAElfDPoS = 1,
        // ReSharper disable once InconsistentNaming
        UpdateAElfDPoS = 2,
        PublishOutValueAndSignature = 3,
        PublishInValue = 4,
    }
}