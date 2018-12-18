using System.Linq;

namespace AElf.Kernel
{
    public partial class TermRounds
    {
        public ulong GetLatestRoundNumber()
        {
            return RoundNumbers.OrderBy(n => n).Last();
        }
    }
}