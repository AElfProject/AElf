using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.Storages
{
    public class NaiveBlockStore : IBlockStore
    {
        private static readonly Dictionary<IHash, IBlock> Transactions = new Dictionary<IHash, IBlock>();

        public Task Insert(IBlock block)
        {
            Transactions.Add(new Hash<ITransaction>(block.CalculateHash()), block);
            return Task.CompletedTask;
        }
    }
}