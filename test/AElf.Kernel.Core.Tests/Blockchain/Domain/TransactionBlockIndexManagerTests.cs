using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Domain
{
    public class TransactionBlockIndexManagerTests : AElfKernelTestBase
    {
        private ITransactionBlockIndexManager _transactionBlockIndexManager;
        public TransactionBlockIndexManagerTests()
        {
            _transactionBlockIndexManager = GetRequiredService<ITransactionBlockIndexManager>();
        }

        [Fact]
        private async Task Set_Get_TransactionBlockIndexAsync_Test()
        {
            var transactionBlockIndex1 = new TransactionBlockIndex()
            {
                BlockHash = HashHelper.ComputeFrom("BlockHash1"),
                BlockHeight = 1L
            };
            var transactionBlockIndex2 = new TransactionBlockIndex()
            {
                BlockHash = HashHelper.ComputeFrom("BlockHash2"),
                BlockHeight = 2L
            };
 
            var transactionId1 = HashHelper.ComputeFrom("transactionId1");
            var transactionId2 = HashHelper.ComputeFrom("transactionId2");

            await _transactionBlockIndexManager.SetTransactionBlockIndexAsync(transactionId1, transactionBlockIndex1);
            var txBlockIndex = await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(transactionId1);
            txBlockIndex.ShouldBe(transactionBlockIndex1);

            
            await _transactionBlockIndexManager.RemoveTransactionIndicesAsync(Enumerable.Repeat(transactionId1, 1));
            var txBlockIndex2 = await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(transactionId1);
            txBlockIndex2.ShouldBeNull();

            var dic = new Dictionary<Hash, TransactionBlockIndex>();
            dic.Add(transactionId1, transactionBlockIndex1);
            dic.Add(transactionId2, transactionBlockIndex2);

            await _transactionBlockIndexManager.SetTransactionBlockIndicesAsync(dic);
            var txBlockIndex3 = await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(transactionId1);
            var txBlockIndex4 = await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(transactionId2);
            txBlockIndex3.ShouldBe(transactionBlockIndex1);
            txBlockIndex4.ShouldBe(transactionBlockIndex2);

            await _transactionBlockIndexManager.RemoveTransactionIndicesAsync(dic.Keys);
            _transactionBlockIndexManager.GetTransactionBlockIndexAsync(transactionId1).Result.ShouldBeNull();
            _transactionBlockIndexManager.GetTransactionBlockIndexAsync(transactionId2).Result.ShouldBeNull();
        }
    }
}