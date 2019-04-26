using System.Collections.Generic;
using AElf.Blockchains.BasicBaseChain;
using AElf.Consensus.AElfConsensus;
using AElf.Consensus.DPoS;
using AElf.Contracts.Consensus.AElfConsensus;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Profit;
using AElf.Contracts.Resource;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.Contracts.Vote;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.SmartContract.Application;
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
            var dto = new OsBlockchainNodeContextStartDto
            {
                ChainId = chainOptions.ChainId,
                ZeroSmartContract = typeof(BasicContractZero)
            };

            var consensusOption = context.ServiceProvider.GetService<IOptionsSnapshot<ConsensusOptions>>().Value;

            var zeroContractAddress = context.ServiceProvider.GetRequiredService<ISmartContractAddressService>()
                .GetZeroSmartContractAddress();

            // Vote Contract
            dto.InitializationSmartContracts.AddGenesisSmartContract<VoteContract>(
                VoteSmartContractAddressNameProvider.Name, GenerateVoteInitializationCallList());

            // Profit Contract
            dto.InitializationSmartContracts.AddGenesisSmartContract<ProfitContract>(
                ProfitSmartContractAddressNameProvider.Name, GenerateProfitInitializationCallList());

            // Election Contract
            dto.InitializationSmartContracts.AddGenesisSmartContract<ElectionContract>(
                ElectionSmartContractAddressNameProvider.Name, GenerateElectionInitializationCallList());

            // Token Contract
            dto.InitializationSmartContracts.AddGenesisSmartContract<TokenContract>(
                TokenSmartContractAddressNameProvider.Name,
                GenerateTokenInitializationCallList(zeroContractAddress, consensusOption.InitialMiners,
                    tokenInitialOptions));

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

            // AElf Consensus Contract
            dto.InitializationSmartContracts.AddConsensusSmartContract<AElfConsensusContract>(
                GenerateConsensusInitializationCallList(consensusOption));

            var osService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
            var that = this;
            AsyncHelper.RunSync(async () => { that.OsBlockchainNodeContext = await osService.StartAsync(dto); });
        }
        
        private SystemTransactionMethodCallList GenerateVoteInitializationCallList()
        {
            var voteContractMethodCallList = new SystemTransactionMethodCallList();
            voteContractMethodCallList.Add(nameof(VoteContract.InitialVoteContract),
                new InitialVoteContractInput
                {
                    // To Lock and Unlock tokens of voters.
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name
                });
            return voteContractMethodCallList;
        }
        
        private SystemTransactionMethodCallList GenerateProfitInitializationCallList()
        {
            var profitContractMethodCallList = new SystemTransactionMethodCallList();
            profitContractMethodCallList.Add(nameof(ProfitContract.InitializeProfitContract),
                new InitializeProfitContractInput
                {
                    // To handle tokens when release profit, add profits and receive profits.
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                });
            return profitContractMethodCallList;
        }

        private SystemTransactionMethodCallList GenerateElectionInitializationCallList()
        {
            var electionContractMethodCallList = new SystemTransactionMethodCallList();
            electionContractMethodCallList.Add(nameof(ElectionContract.InitialElectionContract),
                new InitialElectionContractInput
                {
                    // Create Treasury profit item and register sub items.
                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                    VoteContractSystemName = VoteSmartContractAddressNameProvider.Name,
                    ProfitContractSystemName = ProfitSmartContractAddressNameProvider.Name,
                    
                    // Get current miners.
                    AelfConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name
                });
            return electionContractMethodCallList;
        }
        
        private SystemTransactionMethodCallList GenerateTokenInitializationCallList(Address issuer,
            IReadOnlyCollection<string> tokenReceivers, TokenInitialOptions tokenInitialOptions)
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
                LockWhiteSystemContractNameList =
                {
                    ElectionSmartContractAddressNameProvider.Name,
                    VoteSmartContractAddressNameProvider.Name,
                    ProfitSmartContractAddressNameProvider.Name
                }
            });

            tokenContractCallList.Add(nameof(TokenContract.IssueNativeToken), new IssueNativeTokenInput
            {
                Symbol = tokenInitialOptions.Symbol,
                Amount = (long) (tokenInitialOptions.TotalSupply * tokenInitialOptions.DividendPoolRatio),
                ToSystemContractName = ElectionSmartContractAddressNameProvider.Name,
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

            // Set fee pool address to election contract address.
            tokenContractCallList.Add(nameof(TokenContract.SetFeePoolAddress),
                ElectionSmartContractAddressNameProvider.Name);

            tokenContractCallList.Add(nameof(TokenContract.InitializeTokenContract), new IntializeTokenContractInput
            {
                CrossChainContractSystemName = CrossChainSmartContractAddressNameProvider.Name
            });
            return tokenContractCallList;
        }

        private SystemTransactionMethodCallList GenerateConsensusInitializationCallList(ConsensusOptions consensusOptions)
        {
            var consensusMethodCallList = new SystemTransactionMethodCallList();
            consensusMethodCallList.Add(nameof(AElfConsensusContract.InitialAElfConsensusContract),
                new InitialAElfConsensusContractInput
                {
                    ElectionContractSystemName = ElectionSmartContractAddressNameProvider.Name,
                });
            consensusMethodCallList.Add(nameof(AElfConsensusContract.FirstRound),
                consensusOptions.InitialMiners.ToMiners().GenerateFirstRoundOfNewTerm(consensusOptions.MiningInterval,
                    consensusOptions.StartTimestamp.ToUniversalTime()));
            return consensusMethodCallList;
        }

        private SystemTransactionMethodCallList GenerateParliamentInitializationCallList()
        {
            var parliamentContractCallList = new SystemTransactionMethodCallList();
            parliamentContractCallList.Add(nameof(ParliamentAuthContract.Initialize),
                new ParliamentAuthInitializationInput
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

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            var osService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
            var that = this;
            AsyncHelper.RunSync(() => osService.StopAsync(that.OsBlockchainNodeContext));
        }
    }
}