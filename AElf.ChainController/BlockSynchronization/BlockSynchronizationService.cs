using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
using AElf.Kernel;
using Easy.MessageHub;

namespace AElf.ChainController
{
    public class BlockSynchronizationService : IBlockSynchronizationService
    {
        private readonly IChainService _chainService;
        private readonly IBlockValidationService _blockValidationService;

        private readonly IBlockCollection _blockCollection;
        
        public BlockSynchronizationService(IChainService chainService, IBlockValidationService blockValidationService)
        {
            _chainService = chainService;
            _blockValidationService = blockValidationService;
            
            _blockCollection =  new BlockCollection(_chainService);
        }

        public async Task ReceiveBlock(IBlock block)
        {
            var res = await _blockValidationService.ValidateBlockAsync(block, await GetChainContextAsync());
            MessageHub.Instance.Publish(res);
            if (res == BlockValidationResult.Success)
            {
                await _blockCollection.AddBlock(block);
            }
        }

        private async Task<IChainContext> GetChainContextAsync()
        {
            var chainId = Hash.LoadHex(NodeConfig.Instance.ChainId);
            var blockchain = _chainService.GetBlockChain(chainId);
            IChainContext chainContext = new ChainContext
            {
                ChainId = chainId,
                BlockHash = await blockchain.GetCurrentBlockHashAsync()
            };
            if (chainContext.BlockHash != Hash.Genesis)
            {
                chainContext.BlockHeight = ((BlockHeader)await blockchain.GetHeaderByHashAsync(chainContext.BlockHash)).Index;
            }
            return chainContext;
        }
    }
}