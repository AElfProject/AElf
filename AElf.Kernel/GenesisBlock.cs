using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class GenesisBlock : Block,IGenesisBlock
    {
        public GenesisBlock(ITransaction transactionInit) : base(Hash<IBlock>.Zero)
        {
            TransactionInit = transactionInit;
        }

        public ITransaction TransactionInit { get; }
    }
}