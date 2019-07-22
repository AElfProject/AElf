using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class BlockExecutingServiceTests : SmartContractExecutionExecutingTestBase
    {
        private readonly BlockExecutingService _blockExecutingService;
        private readonly KernelTestHelper _kernelTestHelper;

        public BlockExecutingServiceTests()
        {
            _blockExecutingService = GetRequiredService<BlockExecutingService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task Execute_Block_NonCancellable()
        {
            var txs = BuildTransactions(5);
            var blockHeader = _kernelTestHelper.GenerateBlock(1, Hash.Empty).Header;

            var block = await _blockExecutingService.ExecuteBlockAsync(blockHeader, txs);
            var allTxIds = txs.Select(x => x.GetHash()).ToList();

            block.Body.TransactionsCount.ShouldBe(txs.Count);

            var binaryMerkleTree = new BinaryMerkleTree();
            binaryMerkleTree.AddNodes(allTxIds);
            var merkleTreeRoot = binaryMerkleTree.ComputeRootHash();
            block.Header.MerkleTreeRootOfTransactions.ShouldBe(merkleTreeRoot);

            block.Body.TransactionIds.ShouldBe(allTxIds);
        }

        [Fact]
        public async Task Execute_Block_Cancellable()
        {
            var blockHeader = _kernelTestHelper.GenerateBlock(1, Hash.Empty).Header;
            var nonCancellableTxs = BuildTransactions(5);
            var cancellableTxs = BuildTransactions(5);
            var cancelToken = new CancellationTokenSource();
            cancelToken.Cancel();

            var block = await _blockExecutingService.ExecuteBlockAsync(blockHeader, nonCancellableTxs,
                cancellableTxs, cancelToken.Token);

            var allTxIds = nonCancellableTxs.Select(x => x.GetHash()).ToList();
            allTxIds.Add(cancellableTxs[0].GetHash());
            allTxIds.Add(cancellableTxs[1].GetHash());
            allTxIds.Add(cancellableTxs[2].GetHash());

            var allTxs = new List<Transaction>();
            allTxs.AddRange(nonCancellableTxs);
            allTxs.Add(cancellableTxs[0]);
            allTxs.Add(cancellableTxs[1]);
            allTxs.Add(cancellableTxs[2]);

            block.Body.TransactionsCount.ShouldBe(allTxs.Count);

            var binaryMerkleTree = new BinaryMerkleTree();
            binaryMerkleTree.AddNodes(allTxIds);
            var merkleTreeRoot = binaryMerkleTree.ComputeRootHash();
            block.Header.MerkleTreeRootOfTransactions.ShouldBe(merkleTreeRoot);

            block.Body.TransactionIds.ShouldBe(allTxIds);
        }

        private List<Transaction> BuildTransactions(int txCount)
        {
            var result = new List<Transaction>(txCount);
            
            for (int i = 0; i < txCount; i++)
            {
                result.Add(new Transaction
                {
                    From = AddressHelper.StringToAddress("from"),
                    To = AddressHelper.StringToAddress("to"),
                    MethodName = Guid.NewGuid().ToString()
                });
            }

            return result;
        }
    }
}