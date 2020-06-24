using System.Collections.Generic;
using System.Linq;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Domain
{
    public static class TransactionWithResourceInfoExtensions
    {
        public static HashSet<ScopedStatePath> GetReadOnlyPaths(
            this List<TransactionWithResourceInfo> transactionWithResourceInfos)
        {
            var readPaths =
                new HashSet<ScopedStatePath>(transactionWithResourceInfos.SelectMany(r => r.TransactionResourceInfo.ReadPaths));
            var writePaths =
                new HashSet<ScopedStatePath>(transactionWithResourceInfos.SelectMany(r => r.TransactionResourceInfo.WritePaths));
            readPaths.ExceptWith(writePaths);
            return readPaths;
        }
    }
}