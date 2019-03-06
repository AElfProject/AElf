using AElf.Common;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public interface IStaticChainInformationProvider
    {
        int ChainId { get; }
        Address ZeroSmartContractAddress { get; }
    }
    
    public class StaticChainInformationProvider : IStaticChainInformationProvider, ISingletonDependency
    {
        public int ChainId { get; }
        public Address ZeroSmartContractAddress { get; }

        public StaticChainInformationProvider(IOptionsSnapshot<ChainOptions> chainOptions)
        {
            ChainId = chainOptions.Value.ChainId;
            ZeroSmartContractAddress = Address.BuildContractAddress(ChainId, 0);
        }
    }
}