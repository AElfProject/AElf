using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Blk
{
    public class BlockGenerationService : IBlockGenerationService
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        public BlockGenerationService(IBlockExtraDataService blockExtraDataService)
        {
            _blockExtraDataService = blockExtraDataService;
        }

        public async Task<Block> GenerateBlockAsync(GenerateBlockDto generateBlockDto)
        {
            DateTime currentBlockTime = DateTime.UtcNow;

            var block = new Block
            {
                Header =
                {
                    Height = generateBlockDto.PreBlockHeight,
                    PreviousBlockHash = generateBlockDto.PreBlockHash,
                    ChainId = generateBlockDto.ChainId,
                    Time = Timestamp.FromDateTime(currentBlockTime)
                },
                Body = new BlockBody()
            };
            

            // todo: get block extra data with _blockExtraDataService including consensus data, cross chain data etc.. 
            await _blockExtraDataService.FillBlockExtraData(block);

            // calculate and set tx merkle tree root 
            //block.Complete(currentBlockTime, results);
            return block;
        }

        public void FillBlockAsync(Block block, HashSet<TransactionResult> results)
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
            // Todo: improvement needed?
            block.Body.Complete(block.Header.GetHash());
        }
    }
}