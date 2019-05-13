using System.Collections.Generic;
using AElf.Kernel;

namespace AElf
{
    public interface IBlockBase : IHashProvider
    {
        IEnumerable<Hash> TransactionHashList { get; }
    }
}