using System;
using System.Collections.Generic;

namespace AElf.Kernel.Blockchain.Domain;

[Trait("Category", AElfBlockchainModule)]
public sealed class TransactionResultManagerTests : AElfKernelTestBase
{
    private readonly KernelTestHelper _kernelTestHelper;
    private readonly ITransactionResultManager _transactionResultManager;

    public TransactionResultManagerTests()
    {
        _transactionResultManager = GetRequiredService<ITransactionResultManager>();
        _kernelTestHelper = GetRequiredService<KernelTestHelper>();
    }

    [Fact]
    public async Task RemoveTransactionResultsAsync_BatchDelete_Test()
    {
        var blockHash = HashHelper.ComputeFrom("block1");
        var txIds = new List<Hash>();

        for (var i = 0; i < 3; i++)
        {
            var tx = _kernelTestHelper.GenerateTransaction();
            var txId = tx.GetHash();
            txIds.Add(txId);
            var result = _kernelTestHelper.GenerateTransactionResult(tx, TransactionResultStatus.Mined);
            await _transactionResultManager.AddTransactionResultAsync(result, blockHash);
        }

        foreach (var txId in txIds)
            (await _transactionResultManager.GetTransactionResultAsync(txId, blockHash)).ShouldNotBeNull();

        var blockHashes = new List<Hash> { blockHash, blockHash, blockHash };
        await _transactionResultManager.RemoveTransactionResultsAsync(txIds, blockHashes);

        foreach (var txId in txIds)
            (await _transactionResultManager.GetTransactionResultAsync(txId, blockHash)).ShouldBeNull();
    }

    [Fact]
    public async Task RemoveTransactionResultsAsync_XorKey_Correctness_Test()
    {
        var tx = _kernelTestHelper.GenerateTransaction();
        var txId = tx.GetHash();
        var blockHash1 = HashHelper.ComputeFrom("blockA");
        var blockHash2 = HashHelper.ComputeFrom("blockB");

        var result1 = _kernelTestHelper.GenerateTransactionResult(tx, TransactionResultStatus.Mined);
        var result2 = _kernelTestHelper.GenerateTransactionResult(tx, TransactionResultStatus.Mined);

        await _transactionResultManager.AddTransactionResultAsync(result1, blockHash1);
        await _transactionResultManager.AddTransactionResultAsync(result2, blockHash2);

        await _transactionResultManager.RemoveTransactionResultsAsync(
            new List<Hash> { txId }, new List<Hash> { blockHash1 });

        (await _transactionResultManager.GetTransactionResultAsync(txId, blockHash1)).ShouldBeNull();
        (await _transactionResultManager.GetTransactionResultAsync(txId, blockHash2)).ShouldNotBeNull();
    }

    [Fact]
    public async Task RemoveTransactionResultsAsync_EmptyList_Test()
    {
        await _transactionResultManager.RemoveTransactionResultsAsync(new List<Hash>(), new List<Hash>());
    }

    [Fact]
    public async Task RemoveTransactionResultsAsync_NonExistent_Test()
    {
        var fakeTxId = HashHelper.ComputeFrom("fakeTx");
        var fakeBlockHash = HashHelper.ComputeFrom("fakeBlock");
        await _transactionResultManager.RemoveTransactionResultsAsync(
            new List<Hash> { fakeTxId }, new List<Hash> { fakeBlockHash });
    }

    [Fact]
    public async Task RemoveTransactionResultsAsync_Mixed_Test()
    {
        var blockHash = HashHelper.ComputeFrom("block1");
        var tx = _kernelTestHelper.GenerateTransaction();
        var txId = tx.GetHash();
        var result = _kernelTestHelper.GenerateTransactionResult(tx, TransactionResultStatus.Mined);
        await _transactionResultManager.AddTransactionResultAsync(result, blockHash);

        var fakeTxId = HashHelper.ComputeFrom("nonexistentTx");

        await _transactionResultManager.RemoveTransactionResultsAsync(
            new List<Hash> { txId, fakeTxId },
            new List<Hash> { blockHash, blockHash });

        (await _transactionResultManager.GetTransactionResultAsync(txId, blockHash)).ShouldBeNull();
    }

    [Fact]
    public async Task RemoveTransactionResultsAsync_CountMismatch_Test()
    {
        var txIds = new List<Hash> { HashHelper.ComputeFrom("tx1"), HashHelper.ComputeFrom("tx2") };
        var blockHashes = new List<Hash> { HashHelper.ComputeFrom("block1") };

        await Assert.ThrowsAsync<ArgumentException>(
            () => _transactionResultManager.RemoveTransactionResultsAsync(txIds, blockHashes));
    }
}
