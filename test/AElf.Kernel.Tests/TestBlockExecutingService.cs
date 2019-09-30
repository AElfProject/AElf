using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel
{
    public class TestBlockExecutingService : IBlockExecutingService
    {
        public Task<Block> ExecuteBlockAsync(BlockHeader blockHeader,
            IEnumerable<Transaction> nonCancellableTransactions)
        {
            var block = GenerateBlock(blockHeader, nonCancellableTransactions.Select(p => p.GetHash()));

            return Task.FromResult(block);
        }

        public Task<Block> ExecuteBlockAsync(BlockHeader blockHeader,
            IEnumerable<Transaction> nonCancellableTransactions,
            IEnumerable<Transaction> cancellableTransactions, CancellationToken cancellationToken)
        {
            var block = GenerateBlock(blockHeader, nonCancellableTransactions.Concat(cancellableTransactions)
                .Select(p => p.GetHash()));

            return Task.FromResult(block);
        }

        private Block GenerateBlock(BlockHeader blockHeader, IEnumerable<Hash> transactionIds)
        {
            var leafNodes = transactionIds as Hash[] ?? transactionIds.ToArray();
            blockHeader.MerkleTreeRootOfTransactions = BinaryMerkleTree.FromLeafNodes(leafNodes).Root;
            blockHeader.MerkleTreeRootOfWorldState = Hash.Empty;
            blockHeader.MerkleTreeRootOfTransactionStatus = Hash.Empty;
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