using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Blockchain.Application
{
    public class BlockGenerationService : IBlockGenerationService
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly IStaticChainInformationProvider _staticChainInformationProvider;
        private readonly IBlockchainStateManager _blockchainStateManager;

        public BlockGenerationService(IBlockExtraDataService blockExtraDataService,
            IStaticChainInformationProvider staticChainInformationProvider, IBlockchainStateManager blockchainStateManager)
        {
            _blockExtraDataService = blockExtraDataService;
            _staticChainInformationProvider = staticChainInformationProvider;
            _blockchainStateManager = blockchainStateManager;
        }

        public async Task<Block> GenerateBlockBeforeExecutionAsync(GenerateBlockDto generateBlockDto)
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    ChainId = _staticChainInformationProvider.ChainId,
                    Height = generateBlockDto.PreviousBlockHeight + 1,
                    PreviousBlockHash = generateBlockDto.PreviousBlockHash,
                    Time = generateBlockDto.BlockTime
                },
                Body = new BlockBody()
            };

            // get block extra data with _blockExtraDataService including consensus data, cross chain data etc.. 
            await _blockExtraDataService.FillBlockExtraData(block.Header);
            // calculate and set tx merkle tree root 
            //block.Complete(currentBlockTime, results);
            return block;
        }

        public async Task<Block> FillBlockAfterExecutionAsync(BlockHeader blockHeader, List<Transaction> transactions,
            List<ExecutionReturnSet> blockExecutionReturnSet)
        {
            var bloom = new Bloom();
            var blockStateSet = new BlockStateSet
            {
                BlockHeight = blockHeader.Height,
                PreviousHash = blockHeader.PreviousBlockHash
            };
            foreach (var returnSet in blockExecutionReturnSet)
            {
                foreach (var change in returnSet.StateChanges)
                {
                    blockStateSet.Changes[change.Key] = change.Value;
                }

                if (returnSet.Status == TransactionResultStatus.Mined)
                {
                    bloom.Combine(new[] {new Bloom(returnSet.Bloom.ToByteArray())});    
                }
            }

            blockHeader.Bloom = ByteString.CopyFrom(bloom.Data);
            blockHeader.MerkleTreeRootOfWorldState = CalculateWorldStateMerkleTreeRoot(blockStateSet);
            blockHeader.MerkleTreeRootOfTransactionStatus =
                CalculateTransactionStatusMerkleTreeRoot(blockExecutionReturnSet);
            
            var allExecutedTransactionIds = transactions.Select(x => x.GetHash()).ToList();
            blockHeader.MerkleTreeRootOfTransactions = CalculateTransactionMerkleTreeRoot(allExecutedTransactionIds);
            
            var blockHash = blockHeader.GetHashWithoutCache();
            var blockBody = new BlockBody
            {
                BlockHeader = blockHash
            };
            blockBody.TransactionIds.AddRange(allExecutedTransactionIds);
            
            var block = new Block
            {
                Header = blockHeader,
                Body = blockBody
            };
            blockBody.BlockHeader = blockHash;
            blockStateSet.BlockHash = blockHash;

            await _blockchainStateManager.SetBlockStateSetAsync(blockStateSet);

            return block;
        }

        private Hash CalculateWorldStateMerkleTreeRoot(BlockStateSet blockStateSet)
        {
            Hash merkleTreeRootOfWorldState;
            var byteArrays = GetDeterministicByteArrays(blockStateSet);
            using (var hashAlgorithm = SHA256.Create())
            {
                foreach (var bytes in byteArrays)
                {
                    hashAlgorithm.TransformBlock(bytes, 0, bytes.Length, null, 0);
                }

                hashAlgorithm.TransformFinalBlock(new byte[0], 0, 0);
                merkleTreeRootOfWorldState = Hash.FromByteArray(hashAlgorithm.Hash);
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
        }
        
        private Hash CalculateTransactionStatusMerkleTreeRoot(List<ExecutionReturnSet> blockExecutionReturnSet)
        {
            var executionReturnSet = blockExecutionReturnSet.Select(executionReturn =>
                (executionReturn.TransactionId, executionReturn.Status));
            var nodes = new List<Hash>();
            foreach (var (transactionId, status) in executionReturnSet)
            {
                nodes.Add(GetHashCombiningTransactionAndStatus(transactionId, status));
            }

            return nodes.ComputeBinaryMerkleTreeRootWithLeafNodes();
        }

        private Hash CalculateTransactionMerkleTreeRoot(IEnumerable<Hash> transactionIds)
        {
            return transactionIds.ComputeBinaryMerkleTreeRootWithLeafNodes();
        }
        
        private Hash GetHashCombiningTransactionAndStatus(Hash txId,
            TransactionResultStatus executionReturnStatus)
        {
            // combine tx result status
            var rawBytes = txId.ToByteArray().Concat(Encoding.UTF8.GetBytes(executionReturnStatus.ToString()))
                .ToArray();
            return Hash.FromRawBytes(rawBytes);
        }
    }
}