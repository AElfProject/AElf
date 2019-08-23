using System.Collections.Generic;
using AElf.Kernel;
using AElf.Types;

namespace AElf
{
    public interface IBlockBase : IHashProvider
    {
        IEnumerable<Hash> TransactionIds { get; }
    }
}