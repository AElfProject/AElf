using AElf.Contracts.Parliament;

namespace AElf.Contracts.Configuration
{
    public partial class ConfigurationState
    {
        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentAuthContract { get; set; }
    }
}