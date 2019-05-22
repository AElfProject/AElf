using System.Runtime.CompilerServices;
using Acs4;

[assembly: InternalsVisibleTo("AElf.Kernel.Consensus.AEDPoS")]
namespace AElf.Kernel.Consensus.Infrastructure
{
    internal class ConsensusControlInformation
    {
        public ConsensusCommand ConsensusCommand { get; set; }
    }
}