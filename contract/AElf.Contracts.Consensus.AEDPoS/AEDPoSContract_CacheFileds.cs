namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// DO NOT forget to clear after executing one transaction,
    /// otherwise these cached states will be saved to `executive` instance unexpectedly.
    /// </summary>
    public partial class AEDPoSContract
    {
        private string _processingBlockMinerPubkey;
        private bool? _isMainChain;
    }
}