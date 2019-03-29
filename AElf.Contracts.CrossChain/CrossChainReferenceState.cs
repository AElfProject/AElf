using AElf.Contracts.Consensus.DPoS.SideChain;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken.Messages;

namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal ConsensusContractContainer.ConsensusContractReferenceState ConsensusContract { get; set; }
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
    }
}