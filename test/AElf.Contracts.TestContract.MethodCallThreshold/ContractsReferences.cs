using Acs0;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Treasury;

namespace AElf.Contracts.TestContract.MethodCallThreshold
{
    public partial class MethodCallThresholdContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal TreasuryContractContainer.TreasuryContractReferenceState TreasuryContract { get; set; }
        internal ACS0Container.ACS0ReferenceState Acs0Contract { get; set; }
    }
}