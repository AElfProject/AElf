using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.SmartContract.Domain;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Blockchain.Application
{
    public class GenerateBlockDto
    {
        public Hash PreviousBlockHash { get; set; }
        public long PreviousBlockHeight { get; set; }

        public DateTime BlockTime { get; set; } = DateTime.UtcNow;
    }

    public interface IBlockGenerationService
    {
        Task<Block> GenerateBlockBeforeExecutionAsync(GenerateBlockDto generateBlockDto);

        Task<Block> FillBlockAfterExecutionAsync(BlockHeader blockHeader, List<Transaction> transactions,
            List<ExecutionReturnSet> blockExecutionReturnSet);
    }

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
                    Time = Timestamp.FromDateTime(generateBlockDto.BlockTime)
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
            var merkleTreeRootOfWorldState = ComputeHash(GetDeterministicByteArrays(blockStateSet));
            blockHeader.MerkleTreeRootOfWorldState = merkleTreeRootOfWorldState;
            
            var allExecutedTransactionIds = transactions.Select(x => x.GetHash()).ToList();
            var bmt = new BinaryMerkleTree();
            bmt.AddNodes(allExecutedTransactionIds);
            blockHeader.MerkleTreeRootOfTransactions = bmt.ComputeRootHash();

            _blockExtraDataService.FillMerkleTreeRootExtraDataForTransactionStatus(blockHeader,
                blockExecutionReturnSet.Select(executionReturn =>
                    (executionReturn.TransactionId, executionReturn.Status)));
            
            var blockBody = new BlockBody();
            blockBody.Transactions.AddRange(allExecutedTransactionIds);
            blockBody.TransactionList.AddRange(transactions);
            
            var block = new Block
            {
                Header = blockHeader,
                Body = blockBody
            };
            
            blockBody.BlockHeader = blockHeader.GetHash();
            blockStateSet.BlockHash = blockHeader.GetHash();

            await _blockchainStateManager.SetBlockStateSetAsync(blockStateSet);

            return block;
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

        private Hash ComputeHash(IEnumerable<byte[]> byteArrays)
        {
            using (var hashAlgorithm = SHA256.Create())
            {
                foreach (var bytes in byteArrays)
                {
                    hashAlgorithm.TransformBlock(bytes, 0, bytes.Length, null, 0);
                }

                hashAlgorithm.TransformFinalBlock(new byte[0], 0, 0);
                return Hash.LoadByteArray(hashAlgorithm.Hash);
            }
        }
    }
}