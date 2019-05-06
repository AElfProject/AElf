using Acs0;
using Acs4;
using AElf.Contracts.Consensus.DPoS.SideChain;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;

namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal ConsensusContractContainer.ConsensusContractReferenceState ConsensusContract { get; set; }
        internal ACS0Container.ACS0ReferenceState BasicContractZero { get; set; }
        
        internal ParliamentAuthContractContainer.ParliamentAuthContractReferenceState ParliamentAuthContract { get; set;}
    }
}