using Acs0;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Economic
{
    public class EconomicContractState : ContractState
    {
        public SingletonState<bool> Initialized { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal ElectionContractContainer.ElectionContractReferenceState ElectionContract { get; set; }
        internal ProfitContractContainer.ProfitContractReferenceState ProfitContract { get; set; }
        internal TokenConverterContractContainer.TokenConverterContractReferenceState TokenConverterContract { get; set; }
        internal ParliamentAuthContractContainer.ParliamentAuthContractReferenceState ParliamentAuthContract { get; set; }
        internal ACS0Container.ACS0ReferenceState ZeroContract { get; set; }
    }
}