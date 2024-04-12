namespace AElf.Kernel.Blockchain.Infrastructure;

[Trait("Category", AElfBlockchainModule)]
public class TransactionBlockIndexCacheProviderTests : AElfKernelTestBase
{
    private readonly ITransactionBlockIndexProvider _transactionBlockIndexProvider;

    public TransactionBlockIndexCacheProviderTests()
    {
        _transactionBlockIndexProvider = GetRequiredService<ITransactionBlockIndexProvider>();
    }

    [Fact]
    public void TransactionBlockIndexCacheTest()
    {
        var txId1 = HashHelper.ComputeFrom("TxId1");
        var transactionBlockIndex1 = new TransactionBlockIndex
        {
            BlockHash = HashHelper.ComputeFrom("BlockHash"),
            BlockHeight = 100
        };

        _transactionBlockIndexProvider.AddTransactionBlockIndex(txId1, transactionBlockIndex1);
        var getResult =
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId1, out var cachedTransactionBlockIndex);
        getResult.ShouldBeTrue();
        cachedTransactionBlockIndex.ShouldBe(transactionBlockIndex1);

        var txId2 = HashHelper.ComputeFrom("TxId2");
        var transactionBlockIndex2 = new TransactionBlockIndex
        {
            BlockHash = HashHelper.ComputeFrom("BlockHash2"),
            BlockHeight = 101,
            PreviousExecutionBlockIndexList =
            {
                new BlockIndex { BlockHash = HashHelper.ComputeFrom("PreviousBlockHash"), BlockHeight = 100 }
            }
        };
        _transactionBlockIndexProvider.AddTransactionBlockIndex(txId2, transactionBlockIndex2);

        var txId3 = HashHelper.ComputeFrom("TxId3");
        var transactionBlockIndex3 = new TransactionBlockIndex
        {
            BlockHash = HashHelper.ComputeFrom("BlockHash3"),
            BlockHeight = 101,
            PreviousExecutionBlockIndexList =
            {
                new BlockIndex { BlockHash = HashHelper.ComputeFrom("PreviousBlockHash2"), BlockHeight = 102 },
                new BlockIndex { BlockHash = HashHelper.ComputeFrom("PreviousBlockHash3"), BlockHeight = 103 }
            }
        };
        _transactionBlockIndexProvider.AddTransactionBlockIndex(txId3, transactionBlockIndex3);

        var transactionBlockIndex4 = new TransactionBlockIndex
        {
            BlockHash = HashHelper.ComputeFrom("BlockHash4"),
            BlockHeight = 104,
            PreviousExecutionBlockIndexList =
            {
                new BlockIndex { BlockHash = HashHelper.ComputeFrom("PreviousBlockHash1"), BlockHeight = 101 },
                new BlockIndex { BlockHash = HashHelper.ComputeFrom("PreviousBlockHash2"), BlockHeight = 102 },
                new BlockIndex { BlockHash = HashHelper.ComputeFrom("PreviousBlockHash3"), BlockHeight = 103 }
            }
        };
        _transactionBlockIndexProvider.AddTransactionBlockIndex(txId3, transactionBlockIndex4);

        {
            _transactionBlockIndexProvider.CleanByHeight(99);
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId1, out var cacheBlockIndex1);
            cacheBlockIndex1.ShouldBe(transactionBlockIndex1);
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId2, out var cacheBlockIndex2);
            cacheBlockIndex2.ShouldBe(transactionBlockIndex2);
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId3, out var cacheBlockIndex3);
            cacheBlockIndex3.ShouldBe(transactionBlockIndex4);
        }

        {
            _transactionBlockIndexProvider.CleanByHeight(100);
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId1, out var cacheBlockIndex1);
            cacheBlockIndex1.ShouldBeNull();
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId2, out var cacheBlockIndex2);
            cacheBlockIndex2.ShouldBe(transactionBlockIndex2);
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId3, out var cacheBlockIndex3);
            cacheBlockIndex3.ShouldBe(transactionBlockIndex4);
        }

        {
            _transactionBlockIndexProvider.CleanByHeight(101);
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId1, out var cacheBlockIndex1);
            cacheBlockIndex1.ShouldBeNull();
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId2, out var cacheBlockIndex2);
            cacheBlockIndex2.ShouldBeNull();
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId3, out var cacheBlockIndex3);
            cacheBlockIndex3.ShouldBe(transactionBlockIndex4);
        }

        {
            _transactionBlockIndexProvider.CleanByHeight(102);
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId1, out var cacheBlockIndex1);
            cacheBlockIndex1.ShouldBeNull();
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId2, out var cacheBlockIndex2);
            cacheBlockIndex2.ShouldBeNull();
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId3, out var cacheBlockIndex3);
            cacheBlockIndex3.ShouldBe(transactionBlockIndex4);
        }

        {
            _transactionBlockIndexProvider.CleanByHeight(103);
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId1, out var cacheBlockIndex1);
            cacheBlockIndex1.ShouldBeNull();
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId2, out var cacheBlockIndex2);
            cacheBlockIndex2.ShouldBeNull();
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId3, out var cacheBlockIndex3);
            cacheBlockIndex3.ShouldNotBeNull();
        }

        {
            _transactionBlockIndexProvider.CleanByHeight(104);
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId1, out var cacheBlockIndex1);
            cacheBlockIndex1.ShouldBeNull();
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId2, out var cacheBlockIndex2);
            cacheBlockIndex2.ShouldBeNull();
            _transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId3, out var cacheBlockIndex3);
            cacheBlockIndex3.ShouldBeNull();
        }
    }
}