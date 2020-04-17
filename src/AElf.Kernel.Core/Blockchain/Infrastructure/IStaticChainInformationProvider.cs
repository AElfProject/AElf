using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public interface IStaticChainInformationProvider
    {
        int ChainId { get; }
        Address ZeroSmartContractAddress { get; }
        Address GetZeroSmartContractAddress(int chainId);
    }

    public class StaticChainInformationProvider : IStaticChainInformationProvider, ISingletonDependency
    {
        public int ChainId { get; }
        public Address ZeroSmartContractAddress { get; }

        public Address GetZeroSmartContractAddress(int chainId)
        {
            return BuildZeroContractAddress(chainId);
        }

        public StaticChainInformationProvider(IOptionsSnapshot<ChainOptions> chainOptions)
        {
            ChainId = chainOptions.Value.ChainId;
            ZeroSmartContractAddress = BuildZeroContractAddress(ChainId);
        }

        private static Address BuildZeroContractAddress(int chainId)
        {
            var hash = HashHelper.ConcatAndCompute(HashHelper.ComputeFromInt32(chainId), HashHelper.ComputeFromInt64(0L));
            return Address.FromBytes(hash.ToByteArray());
        }
    }
}