using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Profit
{
    public partial class ProfitContractState
    {
        public SingletonState<Hash> TokenContractSystemName { get; set; }

        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal Acs0.ACS0Container.ACS0ReferenceState BasicContractZero { get; set; }
    }
}