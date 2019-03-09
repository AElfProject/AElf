using System.Linq;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    
    //TODO: move out Round from AElf.Kernel.Types
    // ReSharper disable InconsistentNaming
    public partial class Round
    {
        public long RoundId => RealTimeMinersInformation.Values.Select(bpInfo => bpInfo.ExpectedMiningTime.Seconds).Sum();

    }
}