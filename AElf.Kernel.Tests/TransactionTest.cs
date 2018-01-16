using System.Collections.Generic;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class TransactionTest
    {
        [Fact]
        public void TransactionPipeline()
        {
            Queue<ITransaction> queue = GetTransactions();

        }

        Queue<ITransaction> GetTransactions()
        {
            Queue<ITransaction> txs = new Queue<ITransaction>();
            new List<string> { "a", "e", "l", "f" }.ForEach(
                str => txs.Enqueue(new Transaction() { Data = str }));
            return txs;
        }
    }
}
