using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class GenesisBlock : Block
    {
        public GenesisBlock() : base(Hash<IBlock>.Zero)
        {
        }
    }
}