using System;
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
    public class GenerateBlockDto
    {
        public Hash PreviousBlockHash { get; set; }
        public long PreviousBlockHeight { get; set; }

        public DateTime BlockTime { get; set; } = DateTime.UtcNow;
    }

    public interface IBlockGenerationService
    {
        Task<Block> GenerateBlockBeforeExecutionAsync(GenerateBlockDto generateBlockDto);
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

    }
}