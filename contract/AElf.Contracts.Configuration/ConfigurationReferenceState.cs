using AElf.Contracts.ParliamentAuth;

namespace AElf.Contracts.Configuration
{
    public partial class ConfigurationState
    {
        internal ParliamentAuthContractContainer.ParliamentAuthContractReferenceState ParliamentAuthContract { get; set; }
    }
}