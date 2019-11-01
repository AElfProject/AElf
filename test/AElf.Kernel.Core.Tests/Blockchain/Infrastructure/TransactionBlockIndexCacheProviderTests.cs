using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public class TransactionBlockIndexCacheProviderTests : AElfKernelTestBase
    {
        private readonly ITransactionBlockIndexCacheProvider _transactionBlockIndexCacheProvider;

        public TransactionBlockIndexCacheProviderTests()
        {
            _transactionBlockIndexCacheProvider = GetRequiredService<ITransactionBlockIndexCacheProvider>();
        }

        [Fact]
        public void TransactionBlockIndexCacheTest()
        {
            var txId1 = Hash.FromString("TxId1");
            var transactionBlockIndex1 = new TransactionBlockIndex
            {
                BlockHash = Hash.FromString("BlockHash"),
                BlockHeight = 100
            };

            _transactionBlockIndexCacheProvider.AddOrUpdate(txId1, transactionBlockIndex1);
            var getResult = _transactionBlockIndexCacheProvider.TryGetValue(txId1, out var cachedTransactionBlockIndex);
            getResult.ShouldBeTrue();
            cachedTransactionBlockIndex.ShouldBe(transactionBlockIndex1);

            var txId2 = Hash.FromString("TxId2");
            var transactionBlockIndex2 = new TransactionBlockIndex
            {
                BlockHash = Hash.FromString("BlockHash2"),
                BlockHeight = 101,
                PreviousExecutionBlockIndexList =
                {
                    new BlockIndex {BlockHash = Hash.FromString("PreviousBlockHash"), BlockHeight = 100}
                }
            };
            _transactionBlockIndexCacheProvider.AddOrUpdate(txId2, transactionBlockIndex2);

            var txId3 = Hash.FromString("TxId3");
            var transactionBlockIndex3 = new TransactionBlockIndex
            {
                BlockHash = Hash.FromString("BlockHash3"),
                BlockHeight = 101,
                PreviousExecutionBlockIndexList =
                {
                    new BlockIndex {BlockHash = Hash.FromString("PreviousBlockHash3"), BlockHeight = 101},
                    new BlockIndex {BlockHash = Hash.FromString("PreviousBlockHash2"), BlockHeight = 103}
                }
            };
            _transactionBlockIndexCacheProvider.AddOrUpdate(txId3, transactionBlockIndex3);

            {
                _transactionBlockIndexCacheProvider.CleanByHeight(99);
                _transactionBlockIndexCacheProvider.TryGetValue(txId1, out var cacheBlockIndex1);
                cacheBlockIndex1.ShouldBe(transactionBlockIndex1);
                _transactionBlockIndexCacheProvider.TryGetValue(txId2, out var cacheBlockIndex2);
                cacheBlockIndex2.ShouldBe(transactionBlockIndex2);
                _transactionBlockIndexCacheProvider.TryGetValue(txId3, out var cacheBlockIndex3);
                cacheBlockIndex3.ShouldBe(transactionBlockIndex3);
            }

            {
                _transactionBlockIndexCacheProvider.CleanByHeight(100);
                _transactionBlockIndexCacheProvider.TryGetValue(txId1, out var cacheBlockIndex1);
                cacheBlockIndex1.ShouldBeNull();
                _transactionBlockIndexCacheProvider.TryGetValue(txId2, out var cacheBlockIndex2);
                cacheBlockIndex2.ShouldBe(transactionBlockIndex2);
                _transactionBlockIndexCacheProvider.TryGetValue(txId3, out var cacheBlockIndex3);
                cacheBlockIndex3.ShouldBe(transactionBlockIndex3);
            }

            {
                _transactionBlockIndexCacheProvider.CleanByHeight(101);
                _transactionBlockIndexCacheProvider.TryGetValue(txId1, out var cacheBlockIndex1);
                cacheBlockIndex1.ShouldBeNull();
                _transactionBlockIndexCacheProvider.TryGetValue(txId2, out var cacheBlockIndex2);
                cacheBlockIndex2.ShouldBeNull();
                _transactionBlockIndexCacheProvider.TryGetValue(txId3, out var cacheBlockIndex3);
                cacheBlockIndex3.ShouldBe(transactionBlockIndex3);
            }

            {
                _transactionBlockIndexCacheProvider.CleanByHeight(102);
                _transactionBlockIndexCacheProvider.TryGetValue(txId1, out var cacheBlockIndex1);
                cacheBlockIndex1.ShouldBeNull();
                _transactionBlockIndexCacheProvider.TryGetValue(txId2, out var cacheBlockIndex2);
                cacheBlockIndex2.ShouldBeNull();
                _transactionBlockIndexCacheProvider.TryGetValue(txId3, out var cacheBlockIndex3);
                cacheBlockIndex3.ShouldBe(transactionBlockIndex3);
            }

            {
                _transactionBlockIndexCacheProvider.CleanByHeight(103);
                _transactionBlockIndexCacheProvider.TryGetValue(txId1, out var cacheBlockIndex1);
                cacheBlockIndex1.ShouldBeNull();
                _transactionBlockIndexCacheProvider.TryGetValue(txId2, out var cacheBlockIndex2);
                cacheBlockIndex2.ShouldBeNull();
                _transactionBlockIndexCacheProvider.TryGetValue(txId3, out var cacheBlockIndex3);
                cacheBlockIndex3.ShouldBeNull();
            }
        }
    }
}