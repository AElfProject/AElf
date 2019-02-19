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

        public async Task<Block> GenerateBlockAsync(GenerateBlockDto generateBlockDto)
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
            
            // todo: get block extra data with _blockExtraDataService including consensus data, cross chain data etc.. 
            await _blockExtraDataService.FillBlockExtraData(block);

            // calculate and set tx merkle tree root 
            //block.Complete(currentBlockTime, results);
            return block;
        }
    }
}