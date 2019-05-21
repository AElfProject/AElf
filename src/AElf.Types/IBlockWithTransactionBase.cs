using System.Collections.Generic;
using AElf.Kernel;

namespace AElf
{
    public interface IBlockWithTransactionBase : IHashProvider
    {
        IEnumerable<Transaction> FullTransactionList { get; }
    }
}