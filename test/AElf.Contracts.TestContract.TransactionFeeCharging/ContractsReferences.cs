using AElf.Contracts.MultiToken.Messages;

namespace AElf.Contracts.TestContract.TransactionFeeCharging
{
    public partial class TransactionFeeChargingContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    }
}