using AElf.Contracts.ParliamentAuth;

namespace Configuration
{
    public partial class ConfigurationState
    {
        internal ParliamentAuthContractContainer.ParliamentAuthContractReferenceState ParliamentAuthContract
        {
            get;
            set;
        }
    }
}