using System.Collections.Generic;
using AElf.Kernel;
using AElf.Types;

namespace AElf
{
    public interface IBlockWithTransactionBase : IHashProvider
    {
        IEnumerable<Transaction> FullTransactionList { get; }
    }
}