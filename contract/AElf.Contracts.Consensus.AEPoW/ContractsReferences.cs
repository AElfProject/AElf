using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;

namespace AElf.Contracts.Consensus.AEPoW
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public partial class AEPoWContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }
    }
}