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

        public async Task<long> GetLastIrreversibleBlockHeightAsync()
        {
            var libIdHeight = await GetLibHashAndHeightAsync();
            return libIdHeight.BlockHeight;
        }

        public async Task<Hash> GetLastIrreversibleBlockHashAsync()
        {
            var libIdHeight = await GetLibHashAndHeightAsync();
            return libIdHeight.BlockHash;
        }

        public async Task<LastIrreversibleBlockDto> GetLibHashAndHeightAsync()
        {
            return await _blockchainService.GetLibHashAndHeightAsync();
        }
        
        public async Task<bool> ValidateIrreversibleBlockExistsAsync()
        {
            if (_irreversibleBlockExists)
                return true;
            var lastIrreversibleBlockHeight = await GetLastIrreversibleBlockHeightAsync();
            _irreversibleBlockExists = lastIrreversibleBlockHeight > Constants.GenesisBlockHeight;
            return _irreversibleBlockExists;
        }
    }
}