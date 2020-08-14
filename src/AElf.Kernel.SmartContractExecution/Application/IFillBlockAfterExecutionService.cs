using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IFillBlockAfterExecutionService
    {
        Task<Block> FillAsync(BlockHeader header,
            IEnumerable<Transaction> transactions, ExecutionReturnSetCollection executionReturnSetCollection,
            BlockStateSet blockStateSet);
    }

    public class FillBlockAfterExecutionService : IFillBlockAfterExecutionService, ITransientDependency
    {
        public ILogger<FillBlockAfterExecutionService> Logger { get; set; }

        public virtual Task<Block> FillAsync(BlockHeader header, IEnumerable<Transaction> transactions, ExecutionReturnSetCollection executionReturnSetCollection,
            BlockStateSet blockStateSet)
        {
            Logger.LogTrace("Start block field filling after execution.");
            var bloom = new Bloom();
            foreach (var returnSet in executionReturnSetCollection.Executed)
            {
                bloom.Combine(new[] {new Bloom(returnSet.Bloom.ToByteArray())});
            }
            
            var allExecutedTransactionIds = transactions.Select(x => x.GetHash()).ToList();
            var orderedReturnSets = executionReturnSetCollection.GetExecutionReturnSetList()
                .OrderBy(d => allExecutedTransactionIds.IndexOf(d.TransactionId)).ToList();
            
            var block = new Block
            {
                Header = new BlockHeader(header)
                {
                    Bloom = ByteString.CopyFrom(bloom.Data),
                    MerkleTreeRootOfWorldState = CalculateWorldStateMerkleTreeRoot(blockStateSet),
                    MerkleTreeRootOfTransactionStatus = CalculateTransactionStatusMerkleTreeRoot(orderedReturnSets),
                    MerkleTreeRootOfTransactions = CalculateTransactionMerkleTreeRoot(allExecutedTransactionIds)
                },
                Body = new BlockBody
                {
                    TransactionIds = {allExecutedTransactionIds}
                }
            };
            
            Logger.LogTrace("Finish block field filling after execution.");
            return Task.FromResult(block);
        }
        
        private Hash CalculateWorldStateMerkleTreeRoot(BlockStateSet blockStateSet)
        {
            Logger.LogTrace("Start world state calculation.");
            Hash merkleTreeRootOfWorldState;
            var byteArrays = GetDeterministicByteArrays(blockStateSet);
            using (var hashAlgorithm = SHA256.Create())
            {
                foreach (var bytes in byteArrays)
                {
                    hashAlgorithm.TransformBlock(bytes, 0, bytes.Length, null, 0);
                }

                hashAlgorithm.TransformFinalBlock(new byte[0], 0, 0);
                merkleTreeRootOfWorldState = Hash.LoadFromByteArray(hashAlgorithm.Hash);
            }

            return merkleTreeRootOfWorldState;
        }

        private IEnumerable<byte[]> GetDeterministicByteArrays(BlockStateSet blockStateSet)
        {
            var keys = blockStateSet.Changes.Keys;
            foreach (var k in new SortedSet<string>(keys))
            {
                yield return Encoding.UTF8.GetBytes(k);
                yield return blockStateSet.Changes[k].ToByteArray();
            }

            keys = blockStateSet.Deletes;
            foreach (var k in new SortedSet<string>(keys))
            {
                yield return Encoding.UTF8.GetBytes(k);
                yield return ByteString.Empty.ToByteArray();
            }
        }

        private Hash CalculateTransactionStatusMerkleTreeRoot(List<ExecutionReturnSet> blockExecutionReturnSet)
        {
            Logger.LogTrace("Start transaction status merkle tree root calculation.");
            var executionReturnSet = blockExecutionReturnSet.Select(executionReturn =>
                (executionReturn.TransactionId, executionReturn.Status));
            var nodes = new List<Hash>();
            foreach (var (transactionId, status) in executionReturnSet)
            {
                nodes.Add(GetHashCombiningTransactionAndStatus(transactionId, status));
            }

            return BinaryMerkleTree.FromLeafNodes(nodes).Root;
        }

        private Hash CalculateTransactionMerkleTreeRoot(IEnumerable<Hash> transactionIds)
        {
            Logger.LogTrace("Start transaction merkle tree root calculation.");
            return BinaryMerkleTree.FromLeafNodes(transactionIds).Root;
        }
        
        private Hash GetHashCombiningTransactionAndStatus(Hash txId,
            TransactionResultStatus executionReturnStatus)
        {
            // combine tx result status
            var rawBytes = txId.ToByteArray().Concat(Encoding.UTF8.GetBytes(executionReturnStatus.ToString()))
                .ToArray();
            return HashHelper.ComputeFrom(rawBytes);
        }
    }
}