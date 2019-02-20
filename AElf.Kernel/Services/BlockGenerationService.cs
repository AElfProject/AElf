using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Services;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Services
{
    public class BlockGenerationService : IBlockGenerationService
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        public BlockGenerationService(IBlockExtraDataService blockExtraDataService)
        {
            _blockExtraDataService = blockExtraDataService;
        }

        public Block GenerateBlockAsync(GenerateBlockDto generateBlockDto)
        {
            DateTime currentBlockTime = DateTime.UtcNow;

            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = generateBlockDto.PreviousBlockHeight,
                    PreviousBlockHash = generateBlockDto.PreviousBlockHash,
                    ChainId = generateBlockDto.ChainId,
                    Time = Timestamp.FromDateTime(currentBlockTime)
                },
                Body = new BlockBody()
            };
            return block;
        }

        public async Task FillBlockAsync(Block block, HashSet<TransactionResult> results)
        {
            block.Header.Bloom = ByteString.CopyFrom(
                Bloom.AndMultipleBloomBytes(
                    results.Where(x => !x.Bloom.IsEmpty).Select(x => x.Bloom.ToByteArray())
                )
            );
            // todo: get block extra data with _blockExtraDataService including consensus data, cross chain data etc.. 
            await _blockExtraDataService.FillBlockExtraData(block);
            
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