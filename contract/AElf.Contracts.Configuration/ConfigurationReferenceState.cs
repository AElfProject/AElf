using AElf.Contracts.Parliament;

namespace AElf.Contracts.Configuration
{
    public partial class ConfigurationState
    {
        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }
    }
}