using System.Linq;
using AElf.Common;

namespace AElf.Kernel
{
    public partial class TransactionList
    {
        public string ToDebugString()
        {
            return "[" + string.Join(", ", Transactions.Select(t => t.GetHashBytes().ToHex())) + "]";
        }
    }
}