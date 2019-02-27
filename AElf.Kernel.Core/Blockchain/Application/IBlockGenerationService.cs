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


    public class FillBlockDto
    {
        public List<Transaction> Transactions { get; set; }
        public Hash MerkleTreeRootOfWorldState { get; set; }
        public IEnumerable<byte[]> BloomData { get; set; }
    }
    public interface IBlockGenerationService
    {
        Task<Block> GenerateEmptyBlockAsync(GenerateBlockDto generateBlockDto);
        Task<Block> FillBlockAsync(BlockHeader header, FillBlockDto fillBlockDto);
    }
    
    public class BlockGenerationService : IBlockGenerationService
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        public BlockGenerationService(IBlockExtraDataService blockExtraDataService)
        {
            _blockExtraDataService = blockExtraDataService;
        }

        public async Task<Block> GenerateEmptyBlockAsync(GenerateBlockDto generateBlockDto)
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

        public async Task<Block> FillBlockAsync(BlockHeader header, FillBlockDto fillBlockDto)
        {
            var allExecutedTransactionIds = fillBlockDto.Transactions.Select(x=>x.GetHash()).ToList();
            var bmt = new BinaryMerkleTree();
            bmt.AddNodes(allExecutedTransactionIds);
            header.MerkleTreeRootOfTransactions = bmt.ComputeRootHash();
            header.MerkleTreeRootOfWorldState = fillBlockDto.MerkleTreeRootOfWorldState;
            header.Bloom = ByteString.CopyFrom(Bloom.AndMultipleBloomBytes(fillBlockDto.BloomData));
            
            var body = new BlockBody
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
            body.TransactionList.AddRange(fillBlockDto.Transactions);
            return block;
        }
    }
}