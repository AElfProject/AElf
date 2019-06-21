using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;

namespace AElf.Contracts.Treasury
{
    public partial class TreasuryContractState
    {
        internal ProfitContractContainer.ProfitContractReferenceState ProfitContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal AEDPoSContractContainer.AEDPoSContractReferenceState AEDPoSContract { get; set; }
        internal TokenConverterContractContainer.TokenConverterContractReferenceState TokenConverterContract { get; set; }
        internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
    }
}