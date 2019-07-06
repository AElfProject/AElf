using Acs0;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;

namespace AElf.Contracts.CrossChain
{
    public partial class CrossChainContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal AEDPoSContractContainer.AEDPoSContractReferenceState ConsensusContract { get; set; }
        
        internal ACS0Container.ACS0ReferenceState GenesisContract { get; set; }
        internal ParliamentAuthContractContainer.ParliamentAuthContractReferenceState ParliamentAuthContract { get; set;}
    }
}