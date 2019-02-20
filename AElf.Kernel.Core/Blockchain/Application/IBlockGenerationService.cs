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
        void FillBlockAsync(Block block, List<TransactionResult> results);
        
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
            
            // todo: get block extra data with _blockExtraDataService including consensus data, cross chain data etc.. 
            await _blockExtraDataService.FillBlockExtraData(generateBlockDto.ChainId, block);

            // calculate and set tx merkle tree root 
            //block.Complete(currentBlockTime, results);
            return block;
        }

        public void FillBlockAsync(Block block, List<TransactionResult> results)
        {
            block.Header.Bloom = ByteString.CopyFrom(
                Bloom.AndMultipleBloomBytes(
                    results.Where(x => !x.Bloom.IsEmpty).Select(x => x.Bloom.ToByteArray())
                )
            );
            
            // add tx hash
            block.AddTransactions(results.Select(x => x.TransactionId));
            // set ws merkle tree root
            block.Header.MerkleTreeRootOfWorldState =
                new BinaryMerkleTree().AddNodes(results.Select(x => x.StateHash)).ComputeRootHash();
            
            block.Header.MerkleTreeRootOfTransactions = block.Body.CalculateMerkleTreeRoots();
            block.Body.Complete(block.Header.GetHash());
        }
    }
}