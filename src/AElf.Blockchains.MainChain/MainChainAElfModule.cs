using System;
using System.Collections.Generic;
using AElf.Blockchains.BasicBaseChain;
using AElf.Consensus.DPoS;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Dividend;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Resource;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.Contracts.Vote;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Modularity;
using AElf.OS.Consensus.DPos;
using AElf.OS.Node.Application;
using AElf.OS.Node.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Blockchains.MainChain
{
    [DependsOn(
        typeof(BasicBaseChainAElfModule)
    )]
    public class MainChainAElfModule : AElfModule
    {
        public ILogger<MainChainAElfModule> Logger { get; set; }

        public OsBlockchainNodeContext OsBlockchainNodeContext { get; set; }

        public MainChainAElfModule()
        {
            Logger = NullLogger<MainChainAElfModule>.Instance;
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var tokenInitialOptions = context.ServiceProvider.GetService<IOptionsSnapshot<TokenInitialOptions>>().Value;
            var chainOptions = context.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value;
            var dto = new OsBlockchainNodeContextStartDto()
            {
                ChainId = chainOptions.ChainId,
                ZeroSmartContract = typeof(BasicContractZero)
            };

            var dposOptions = context.ServiceProvider.GetService<IOptionsSnapshot<DPoSOptions>>().Value;
            var zeroContractAddress = context.ServiceProvider.GetRequiredService<ISmartContractAddressService>()
                .GetZeroSmartContractAddress();

            // Consensus Contract
            dto.InitializationSmartContracts.AddConsensusSmartContract<ConsensusContract>(
                GenerateConsensusInitializationCallList(dposOptions, tokenInitialOptions));

            // Dividend Contract
            dto.InitializationSmartContracts.AddGenesisSmartContract<DividendContract>(
                DividendSmartContractAddressNameProvider.Name, GenerateDividendInitializationCallList());

            // Token Contract
            dto.InitializationSmartContracts.AddGenesisSmartContract<TokenContract>(
                TokenSmartContractAddressNameProvider.Name,
                GenerateTokenInitializationCallList(zeroContractAddress, dposOptions.InitialMiners, tokenInitialOptions));

            // Resource Contract
            dto.InitializationSmartContracts.AddGenesisSmartContract<ResourceContract>(
                ResourceSmartContractAddressNameProvider.Name);

            // Fee Receiver Contract
            dto.InitializationSmartContracts.AddGenesisSmartContract<FeeReceiverContract>(
                ResourceFeeReceiverSmartContractAddressNameProvider.Name);

            // Parliament Contract
            dto.InitializationSmartContracts.AddGenesisSmartContract<ParliamentAuthContract>(
                ParliamentAuthContractAddressNameProvider.Name, GenerateParliamentInitializationCallList());
            
            // Cross Chain Contract
            dto.InitializationSmartContracts.AddGenesisSmartContract<CrossChainContract>(
                CrossChainSmartContractAddressNameProvider.Name, GenerateCrossChainInitializationCallList());

            // Vote Contract
            dto.InitializationSmartContracts.AddGenesisSmartContract<VoteContract>(
                VoteSmartContractAddressNameProvider.Name, GenerateVoteInitializationCallList());

            // Election Contract
            // dto.InitializationSmartContracts.AddGenesisSmartContract<ElectionContract>(
            //    ElectionSmartContractAddressNameProvider.Name, GenerateElectionInitializationCallList());

            var osService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
            var that = this;
            AsyncHelper.RunSync(async () => { that.OsBlockchainNodeContext = await osService.StartAsync(dto); });
        }

        private SystemTransactionMethodCallList GenerateConsensusInitializationCallList(DPoSOptions dposOptions,
            TokenInitialOptions tokenInitialOptions)
        {
            var consensusMethodCallList = new SystemTransactionMethodCallList();
            consensusMethodCallList.Add(nameof(ConsensusContract.InitialDPoSContract),
                new InitialDPoSContractInput
                {
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    DividendsContractSystemName = DividendSmartContractAddressNameProvider.Name,
                    LockTokenForElection = tokenInitialOptions.LockForElection
                });
            consensusMethodCallList.Add(nameof(ConsensusContract.InitialConsensus),
                dposOptions.InitialMiners.ToMiners().GenerateFirstRoundOfNewTerm(dposOptions.MiningInterval,
                    dposOptions.StartTimestamp.ToUniversalTime()));
            consensusMethodCallList.Add(nameof(ConsensusContract.ConfigStrategy),
                new DPoSStrategyInput
                {
                    IsBlockchainAgeSettable = dposOptions.IsBlockchainAgeSettable,
                    IsTimeSlotSkippable = dposOptions.IsTimeSlotSkippable,
                    IsVerbose = dposOptions.Verbose
                });
            return consensusMethodCallList;
        }

        private SystemTransactionMethodCallList GenerateDividendInitializationCallList()
        {
            var dividendMethodCallList = new SystemTransactionMethodCallList();
            dividendMethodCallList.Add(nameof(DividendContract.InitializeDividendContract),
                new InitialDividendContractInput
                {
                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name
                });
            return dividendMethodCallList;
        }

        private SystemTransactionMethodCallList GenerateTokenInitializationCallList(Address issuer,
            List<string> tokenReceivers, TokenInitialOptions tokenInitialOptions)
        {
            var tokenContractCallList = new SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.CreateNativeToken), new CreateNativeTokenInput
            {
                Symbol = tokenInitialOptions.Symbol,
                Decimals = tokenInitialOptions.Decimals,
                IsBurnable = tokenInitialOptions.IsBurnable,
                TokenName = tokenInitialOptions.Name,
                TotalSupply = tokenInitialOptions.TotalSupply,
                // Set the contract zero address as the issuer temporarily.
                Issuer = issuer,
                LockWhiteSystemContractNameList = {ConsensusSmartContractAddressNameProvider.Name}
            });

            tokenContractCallList.Add(nameof(TokenContract.IssueNativeToken), new IssueNativeTokenInput
            {
                Symbol = tokenInitialOptions.Symbol,
                Amount = (long) (tokenInitialOptions.TotalSupply * tokenInitialOptions.DividendPoolRatio),
                ToSystemContractName = DividendSmartContractAddressNameProvider.Name,
                Memo = "Set dividends.",
            });

            //TODO: Maybe should be removed after testing.
            foreach (var tokenReceiver in tokenReceivers)
            {
                tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
                {
                    Symbol = tokenInitialOptions.Symbol,
                    Amount = (long) (tokenInitialOptions.TotalSupply * (1 - tokenInitialOptions.DividendPoolRatio)) /
                             tokenReceivers.Count,
                    To = Address.FromPublicKey(ByteArrayHelpers.FromHexString(tokenReceiver)),
                    Memo = "Set initial miner's balance.",
                });
            }

            // Set fee pool address to dividend contract address.
            tokenContractCallList.Add(nameof(TokenContract.SetFeePoolAddress),
                DividendSmartContractAddressNameProvider.Name);

            tokenContractCallList.Add(nameof(TokenContract.InitializeTokenContract), new IntializeTokenContractInput
            {
                CrossChainContractSystemName = CrossChainSmartContractAddressNameProvider.Name
            });
            return tokenContractCallList;
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

        private SystemTransactionMethodCallList GenerateCrossChainInitializationCallList()
        {
            var crossChainMethodCallList = new SystemTransactionMethodCallList();
            crossChainMethodCallList.Add(nameof(CrossChainContract.Initialize),
                new AElf.Contracts.CrossChain.InitializeInput
                {
                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name
                });
            return crossChainMethodCallList;
        }

        private SystemTransactionMethodCallList GenerateVoteInitializationCallList()
        {
            var voteContractMethodCallList = new SystemTransactionMethodCallList();
            voteContractMethodCallList.Add(nameof(VoteContract.InitialVoteContract),
                new InitialVoteContractInput
                {
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name
                });
            return voteContractMethodCallList;
        }

        private SystemTransactionMethodCallList GenerateElectionInitializationCallList()
        {
            var electionContractMethodCallList = new SystemTransactionMethodCallList();
            electionContractMethodCallList.Add(nameof(ElectionContract.InitialElectionContract),
                new InitialElectionContractInput
                {
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    VoteContractSystemName = VoteSmartContractAddressNameProvider.Name
                });
            return electionContractMethodCallList;
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            var osService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
            var that = this;
            AsyncHelper.RunSync(() => osService.StopAsync(that.OsBlockchainNodeContext));
        }
    }
}