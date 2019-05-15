using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContractState
    {
        public SingletonState<Hash> TokenContractSystemName { get; set; }

        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
    }
}