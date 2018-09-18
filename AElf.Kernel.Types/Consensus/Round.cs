using System.Linq;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class Round
    {
        public Hash RoundId =>
            HashExtensions.CalculateHashOfHashList(BlockProducers.Values.Select(bpInfo => bpInfo.Signature).ToArray());
    }
}