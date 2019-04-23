using System;
using AElf.Blockchains.BasicBaseChain;
using AElf.Consensus.DPoS;
using AElf.Contracts.Consensus.DPoS.SideChain;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.CrossChain;
using AElf.CrossChain.Grpc;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Token;
using AElf.Modularity;
using AElf.OS.Node.Application;
using AElf.OS.Node.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Blockchains.SideChain
{
    [DependsOn(
        typeof(BasicBaseChainAElfModule)
    )]
    public class SideChainAElfModule : AElfModule
    {
        public ILogger<SideChainAElfModule> Logger { get; set; }

        public OsBlockchainNodeContext OsBlockchainNodeContext { get; set; }

        public SideChainAElfModule()
        {
            Logger = NullLogger<SideChainAElfModule>.Instance;
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var chainOptions = context.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value;
            var dto = new OsBlockchainNodeContextStartDto()
            {
                ChainId = chainOptions.ChainId,
                ZeroSmartContract = typeof(BasicContractZero)
            };
            var chainInitializationPlugin = context.ServiceProvider.GetService<IChainInitializationPlugin>();
            var chainInitializationContext = AsyncHelper.RunSync(async () =>
                await chainInitializationPlugin.RequestChainInitializationContextAsync(dto.ChainId));
            
            var dposOptions = context.ServiceProvider.GetService<IOptionsSnapshot<DPoSOptions>>().Value;

            dto.InitializationSmartContracts.AddConsensusSmartContract<ConsensusContract>(
                GenerateConsensusInitializationCallList(dposOptions, chainInitializationContext));

            dto.InitializationSmartContracts.AddGenesisSmartContract<TokenContract>(
                TokenSmartContractAddressNameProvider.Name, GenerateTokenInitializationCallList());
            
            dto.InitializationSmartContracts.AddGenesisSmartContract<ParliamentAuthContract>(
                ParliamentAuthContractAddressNameProvider.Name, GenerateParliamentInitializationCallList());
            
            var crossChainOption = context.ServiceProvider.GetService<IOptionsSnapshot<CrossChainConfigOption>>()
                .Value;
            int parentChainId = crossChainOption.ParentChainId;
            var crossChainMethodCallList = GenerateCrossChainInitializationCallList(parentChainId);
            dto.InitializationSmartContracts.AddGenesisSmartContract<CrossChainContract>(
                CrossChainSmartContractAddressNameProvider.Name, crossChainMethodCallList);

            var osService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
            var that = this;
            AsyncHelper.RunSync(async () => { that.OsBlockchainNodeContext = await osService.StartAsync(dto); });
        }

        private SystemTransactionMethodCallList GenerateTokenInitializationCallList()
        {
            var tokenContractCallList = new SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.InitializeTokenContract), new IntializeTokenContractInput
            {
                CrossChainContractSystemName = CrossChainSmartContractAddressNameProvider.Name
            });
            return tokenContractCallList;
        }
        
        private SystemTransactionMethodCallList GenerateConsensusInitializationCallList(DPoSOptions dposOptions, ChainInitializationContext chainInitializationContext)
        {
            var consensusMethodCallList = new SystemTransactionMethodCallList();
            
            var miners = chainInitializationContext == null
                ? dposOptions.InitialMiners.ToMiners()
                : MinerListWithRoundNumber.Parser.ParseFrom(chainInitializationContext.ExtraInformation[0]).MinerList;
            var timestamp = chainInitializationContext?.CreatedTime.ToDateTime() ?? dposOptions.StartTimestamp;
            consensusMethodCallList.Add(nameof(ConsensusContract.InitialConsensus),
                miners.GenerateFirstRoundOfNewTerm(dposOptions.MiningInterval, timestamp.ToUniversalTime()));
            consensusMethodCallList.Add(nameof(ConsensusContract.ConfigStrategy),
                new DPoSStrategyInput
                {
                    IsBlockchainAgeSettable = dposOptions.IsBlockchainAgeSettable,
                    IsTimeSlotSkippable = dposOptions.IsTimeSlotSkippable,
                    IsVerbose = dposOptions.Verbose
                });
            return consensusMethodCallList;
        }

        private SystemTransactionMethodCallList GenerateCrossChainInitializationCallList(int parentChainId)
        {
            var crossChainMethodCallList = new SystemTransactionMethodCallList();
            crossChainMethodCallList.Add(nameof(CrossChainContract.Initialize), new AElf.Contracts.CrossChain.InitializeInput
            {
                ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
                TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                ParliamentContractSystemName = ParliamentAuthContractAddressNameProvider.Name,
                ParentChainId = parentChainId
            });
            return crossChainMethodCallList;
        }
        
        private SystemTransactionMethodCallList GenerateParliamentInitializationCallList()
        {
            var parliamentContractCallList = new SystemTransactionMethodCallList();
            parliamentContractCallList.Add(nameof(ParliamentAuthContract.Initialize), new ParliamentAuthInitializationInput
            {
                ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name
            });
            return parliamentContractCallList;
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            var osService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
            var that = this;
            AsyncHelper.RunSync(() => osService.StopAsync(that.OsBlockchainNodeContext));
        }
    }
}