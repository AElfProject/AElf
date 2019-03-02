using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Blockchain.Application
{

    public class GenerateBlockDto
    {
        public Hash PreviousBlockHash { get; set; }
        public ulong PreviousBlockHeight { get; set; }
        
        public DateTime BlockTime { get; set; } = DateTime.UtcNow;
        
    }
    
    public interface IBlockGenerationService
    {
        Task<Block> GenerateBlockBeforeExecutionAsync(GenerateBlockDto generateBlockDto);
        Task<Block> FillBlockAfterExecutionAsync(BlockHeader blockHeader, List<Transaction> transactions, Hash merkleTreeRootOfWorldState);
        
    }
    
    public class BlockGenerationService : IBlockGenerationService
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        public BlockGenerationService(IBlockExtraDataService blockExtraDataService)
        {
            _blockExtraDataService = blockExtraDataService;
        }

        public async Task<Block> GenerateBlockBeforeExecutionAsync(GenerateBlockDto generateBlockDto)
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = generateBlockDto.PreviousBlockHeight + 1,
                    PreviousBlockHash = generateBlockDto.PreviousBlockHash,
                    Time = Timestamp.FromDateTime(generateBlockDto.BlockTime)
                },
                Body = new BlockBody()
            };
            
            // calculate and set tx merkle tree root 
            //block.Complete(currentBlockTime, results);
            return block;
        }

        public async Task<Block> FillBlockAfterExecutionAsync(BlockHeader blockHeader, List<Transaction> transactions, Hash merkleTreeRootOfWorldState)
        {
//            block.Header.Bloom = ByteString.CopyFrom(
//                Bloom.AndMultipleBloomBytes(
//                    results.Where(x => !x.Bloom.IsEmpty).Select(x => x.Bloom.ToByteArray())
//                )
//            );
            
            var allExecutedTransactionIds = transactions.Select(x=>x.GetHash()).ToList();
            var bmt = new BinaryMerkleTree();
            bmt.AddNodes(allExecutedTransactionIds);
            blockHeader.MerkleTreeRootOfTransactions = bmt.ComputeRootHash();
            blockHeader.MerkleTreeRootOfWorldState = merkleTreeRootOfWorldState;
            
            var blockBody = new BlockBody();
            blockBody.Transactions.AddRange(allExecutedTransactionIds);
            blockBody.TransactionList.AddRange(transactions);
            
            var block = new Block
            {
                Header = blockHeader,
                Body = blockBody
            };
            // get block extra data with _blockExtraDataService including consensus data, cross chain data etc.. 
            await _blockExtraDataService.FillBlockExtraData(blockHeader.ChainId, block);
            blockBody.BlockHeader = blockHeader.GetHash();
            
            return block;
        }
    }
}