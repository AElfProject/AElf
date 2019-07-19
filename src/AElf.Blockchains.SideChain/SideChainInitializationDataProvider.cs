using System.Threading.Tasks;
using Acs7;
using AElf.CrossChain;
using AElf.CrossChain.Communication;
using AElf.Kernel;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Blockchains.SideChain
{
    public class SideChainInitializationDataProvider : ISideChainInitializationDataProvider, ISingletonDependency
    {
        private readonly ChainOptions _chainOptions;
        private readonly IChainInitializationDataPlugin _chainInitializationDataPlugin;

        public SideChainInitializationDataProvider(IOptionsSnapshot<ChainOptions> chainOptions, 
            IOptionsSnapshot<CrossChainConfigOptions> crossChainConfigOptions, 
            IChainInitializationDataPlugin chainInitializationDataPlugin)
        {
            _chainOptions = chainOptions.Value;
            _chainInitializationDataPlugin = chainInitializationDataPlugin;
            ParentChainId = crossChainConfigOptions.Value.ParentChainId;
        }

        public async Task<ChainInitializationData> GetChainInitializationDataAsync()
        {
            return await _chainInitializationDataPlugin.GetChainInitializationDataAsync(_chainOptions.ChainId);
        }

        public int ParentChainId { get; }
    }
}