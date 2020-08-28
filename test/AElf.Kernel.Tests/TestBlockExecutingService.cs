using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel
{
    public class TestBlockExecutingService : IBlockExecutingService
    {
        public Task<BlockExecutedSet> ExecuteBlockAsync(BlockHeader blockHeader,
            List<Transaction> nonCancellableTransactions)
        {
            var block = GenerateBlock(blockHeader, nonCancellableTransactions.Select(p => p.GetHash()));

            return Task.FromResult(new BlockExecutedSet(){Block = block});
        }

        public Task<BlockExecutedSet> ExecuteBlockAsync(BlockHeader blockHeader,
            IEnumerable<Transaction> nonCancellableTransactions,
            IEnumerable<Transaction> cancellableTransactions, CancellationToken cancellationToken)
        {
            var transactions = cancellationToken.IsCancellationRequested
                ? nonCancellableTransactions.ToList()
                : nonCancellableTransactions.Concat(cancellableTransactions).ToList();

            var block = GenerateBlock(blockHeader, transactions.Select(p => p.GetHash()));

            return Task.FromResult(new BlockExecutedSet() {Block = block});
        }

        private Block GenerateBlock(BlockHeader blockHeader, IEnumerable<Hash> transactionIds)
        {
            
            var leafNodes = transactionIds as Hash[] ?? transactionIds.ToArray();
            blockHeader.MerkleTreeRootOfTransactions = BinaryMerkleTree.FromLeafNodes(leafNodes).Root;
            blockHeader.MerkleTreeRootOfWorldState = Hash.Empty;
            blockHeader.MerkleTreeRootOfTransactionStatus = Hash.Empty;
            if (blockHeader.SignerPubkey.IsEmpty)
                blockHeader.SignerPubkey = ByteString.CopyFromUtf8("SignerPubkey");
            
            var block = new Block
            {
                Header = blockHeader,
                Body = new BlockBody()
            };
            
            block.Body.TransactionIds.AddRange(leafNodes);

            return block;
        }
    }
}