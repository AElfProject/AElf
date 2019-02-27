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
        public int ChainId { get; set; }
        public Hash PreviousBlockHash { get; set; }
        public ulong PreviousBlockHeight { get; set; }
        
        public DateTime BlockTime { get; set; } = DateTime.UtcNow;
        
    }
    
    public interface IBlockGenerationService
    {
        Task<Block> GenerateBlockAsync(GenerateBlockDto generateBlockDto);
        Task<Block> FillBlockAsync(BlockHeader header, List<Transaction> transactions, Hash merkleTreeRootOfWorldState, IEnumerable<byte[]> bloomData);
        
    }
    
    public class BlockGenerationService : IBlockGenerationService
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        public BlockGenerationService(IBlockExtraDataService blockExtraDataService)
        {
            _blockExtraDataService = blockExtraDataService;
        }

        public async Task<Block> GenerateBlockAsync(GenerateBlockDto generateBlockDto)
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = generateBlockDto.PreviousBlockHeight + 1,
                    PreviousBlockHash = generateBlockDto.PreviousBlockHash,
                    ChainId = generateBlockDto.ChainId,
                    Time = Timestamp.FromDateTime(generateBlockDto.BlockTime)
                },
                Body = new BlockBody()
            };
            
            // calculate and set tx merkle tree root 
            //block.Complete(currentBlockTime, results);
            return block;
        }

        public async Task<Block> FillBlockAsync(BlockHeader header, List<Transaction> transactions,
            Hash merkleTreeRootOfWorldState, IEnumerable<byte[]> bloomData)
        {
            var allExecutedTransactionIds = transactions.Select(x=>x.GetHash()).ToList();
            var bmt = new BinaryMerkleTree();
            bmt.AddNodes(allExecutedTransactionIds);
            header.MerkleTreeRootOfTransactions = bmt.ComputeRootHash();
            header.MerkleTreeRootOfWorldState = merkleTreeRootOfWorldState;
            header.Bloom = ByteString.CopyFrom(Bloom.AndMultipleBloomBytes(bloomData));
            
            var body = new BlockBody()
            {
                BlockHeader = header.GetHash()
            };
            var block = new Block
            {
                Header = header,
                Body = body
            };
            
            // get block extra data with _blockExtraDataService including consensus data, cross chain data etc.. 
            await _blockExtraDataService.FillBlockExtraData(header.ChainId, block);
            
            // add tx hash
            body.Transactions.AddRange(allExecutedTransactionIds);
            body.TransactionList.AddRange(transactions);
            return block;
        }
    }
}