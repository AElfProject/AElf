using System.Threading.Tasks;
using Acs7;
using AElf.Blockchains.ContractInitialization;
using AElf.CrossChain;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.ContractTestBase
{
    public class SideChainInitializationDataProvider : ISideChainInitializationDataProvider, ISingletonDependency
    {
        public SideChainInitializationDataProvider(IOptionsSnapshot<CrossChainConfigOptions> crossChainConfigOptions)
        {
            ParentChainId = ChainHelper.ConvertBase58ToChainId(crossChainConfigOptions.Value.ParentChainId);
        }

        public async Task<ChainInitializationData> GetChainInitializationDataAsync()
        {
            // Default Initialization Data
            return new ChainInitializationData();
        }

        public int ParentChainId { get; }
    }
}