using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Infrastructure;

namespace AElf.Kernel.Blockchain.Application
{
    public class BlockGenerationService : IBlockGenerationService
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly IStaticChainInformationProvider _staticChainInformationProvider;

        public BlockGenerationService(IBlockExtraDataService blockExtraDataService,
            IStaticChainInformationProvider staticChainInformationProvider)
        {
            _blockExtraDataService = blockExtraDataService;
            _staticChainInformationProvider = staticChainInformationProvider;
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

        
    }
}