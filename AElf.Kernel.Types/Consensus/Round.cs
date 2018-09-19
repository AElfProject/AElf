using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class Round
    {
        public long RoundId => BlockProducers.Values.Select(bpInfo => bpInfo.TimeSlot.Seconds).Sum();
    }
}