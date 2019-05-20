using AElf.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Blockchains.BasicBaseChain
{
    public interface IOptionsProvider
    {
        ChainOptions ChainOptions { get; }
        ConsensusOptions ConsensusOptions { get; }
        ContractOptions ContractOptions { get; }
        TokenInitialOptions TokenInitialOptions { get; }
        CrossChainConfigOption CrossChainConfigOption { get; }
    }

    public class OptionsProvider : IOptionsProvider, ITransientDependency
    {
        public ChainOptions ChainOptions { get; }

        public ConsensusOptions ConsensusOptions { get; }
        public ContractOptions ContractOptions { get; }
        public TokenInitialOptions TokenInitialOptions { get; }
        public CrossChainConfigOption CrossChainConfigOption { get; }

        public OptionsProvider(IOptionsSnapshot<ChainOptions> chainOptions,
            IOptionsSnapshot<ConsensusOptions> consensusOptions,
            IOptionsSnapshot<CrossChainConfigOption> crossChainConfigOptions,
            IOptionsSnapshot<ContractOptions> contractOptions, IOptionsSnapshot<TokenInitialOptions> tokenInitialOptions)
        {
            ChainOptions = chainOptions.Value;
            ConsensusOptions = consensusOptions.Value;
            CrossChainConfigOption = crossChainConfigOptions.Value;
            TokenInitialOptions = tokenInitialOptions.Value;
            ContractOptions = contractOptions.Value;
        }
    }
}