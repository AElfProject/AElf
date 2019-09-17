using System.Collections.Generic;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        private readonly Dictionary<long, Round> _rounds = new Dictionary<long, Round>();
        private string _processingBlockMinerPubkey;
        private bool? _isMainChain;
    }
}