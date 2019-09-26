using System.Collections.Generic;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// DO NOT forget to clear after executing one transaction,
    /// otherwise these cached states will be saved to `executive` instance wrongly.
    /// </summary>
    public partial class AEDPoSContract
    {
        private readonly Dictionary<long, Round> _rounds = new Dictionary<long, Round>();
        private string _processingBlockMinerPubkey;
        private bool? _isMainChain;
    }
}