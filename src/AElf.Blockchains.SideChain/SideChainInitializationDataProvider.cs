using System;
using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain;
using AElf.CrossChain.Communication;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Blockchains.SideChain
{
    public class SideChainInitializationDataProvider : ISideChainInitializationDataProvider, ISingletonDependency
    {
        private readonly ChainOptions _chainOptions;
        private readonly IChainInitializationDataPlugin _chainInitializationDataPlugin;
        private readonly IBlockchainService _blockchainService;

        public SideChainInitializationDataProvider(IOptionsSnapshot<ChainOptions> chainOptions, 
            IOptionsSnapshot<CrossChainConfigOptions> crossChainConfigOptions, 
            IChainInitializationDataPlugin chainInitializationDataPlugin, IBlockchainService blockchainService)
        {
            _chainOptions = chainOptions.Value;
            _chainInitializationDataPlugin = chainInitializationDataPlugin;
            _blockchainService = blockchainService;
            ParentChainId = ChainHelper.ConvertBase58ToChainId(crossChainConfigOptions.Value.ParentChainId);
        }

        public async Task<ChainInitializationData> GetChainInitializationDataAsync()
        {
            var chainInitializationData =
                await _chainInitializationDataPlugin.GetChainInitializationDataAsync(_chainOptions.ChainId);
            if (chainInitializationData != null)
                return chainInitializationData;
            var chain = await _blockchainService.GetChainAsync();
            if (chain == null)
                throw new Exception("Initialization data cannot be null for a new side chain.");
            return null;
        }

        public int ParentChainId { get; }
    }
}