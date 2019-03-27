using AElf.Contracts.Dividend;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken.Messages;

namespace AElf.Contracts.Consensus.DPoS
{
    public partial class DPoSContractState
    {
        internal DividendContractContainer.DividendContractReferenceState DividendContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal BasicContractZeroContainer.BasicContractZeroReferenceState BasicContractZero { get; set; }
    }
}