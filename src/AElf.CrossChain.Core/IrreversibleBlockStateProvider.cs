using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;

namespace AElf.CrossChain
{
    public class IrreversibleBlockStateProvider : IIrreversibleBlockStateProvider
    {
        private readonly IBlockchainService _blockchainService;
        private bool _irreversibleBlockExists;

        public IrreversibleBlockStateProvider(IBlockchainService blockchainService)
        {
            _blockchainService = blockchainService;
        }

        public async Task<Block> GetIrreversibleBlockByHeightAsync(long height)
        {
            return await _blockchainService.GetIrreversibleBlockByHeightAsync(height);
        }

        public async Task<LastIrreversibleBlockDto> GetLastIrreversibleBlockHashAndHeightAsync()
        {
            return await _blockchainService.GetLibHashAndHeightAsync();
        }
        
        public async Task<bool> ValidateIrreversibleBlockExistingAsync()
        {
            if (_irreversibleBlockExists)
                return true;
            var libIdHeight = await GetLastIrreversibleBlockHashAndHeightAsync();
            var lastIrreversibleBlockHeight = libIdHeight.BlockHeight;
            _irreversibleBlockExists = lastIrreversibleBlockHeight > Constants.GenesisBlockHeight;
            return _irreversibleBlockExists;
        }
    }
}