using System.Collections.Generic;
using AElf.Blockchains.BasicBaseChain;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS.SideChain;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Dividend;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Resource;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.SmartContract;
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
            
//            var dividendMethodCallList = new SystemTransactionMethodCallList();
//            dividendMethodCallList.Add(nameof(DividendContract.InitializeWithContractSystemNames),
//                new AElf.Contracts.Dividend.InitializeWithContractSystemNamesInput
//                {
//                    ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
//                    TokenContractSystemName = TokenSmartContractAddressNameProvider.Name
//                });

            dto.InitializationSmartContracts.AddConsensusSmartContract<ConsensusContract>();

            var zeroContractAddress = context.ServiceProvider.GetRequiredService<ISmartContractAddressService>()
                .GetZeroSmartContractAddress();
//            dto.InitializationSmartContracts.AddGenesisSmartContract<DividendContract>(
//                DividendsSmartContractAddressNameProvider.Name, dividendMethodCallList);
            dto.InitializationSmartContracts.AddGenesisSmartContract<TokenContract>(
                TokenSmartContractAddressNameProvider.Name,
                GenerateTokenInitializationCallList(zeroContractAddress,
                    context.ServiceProvider.GetService<IOptions<DPoSOptions>>().Value.InitialMiners));
//            dto.InitializationSmartContracts.AddGenesisSmartContract<ResourceContract>(
//                ResourceSmartContractAddressNameProvider.Name);
//            dto.InitializationSmartContracts.AddGenesisSmartContract<FeeReceiverContract>(
//                ResourceFeeReceiverSmartContractAddressNameProvider.Name);
            
            var crossChainOption = context.ServiceProvider.GetService<IOptionsSnapshot<CrossChainConfigOption>>()
                .Value;
            int parentChainId = crossChainOption.ParentChainId;
            var crossChainMethodCallList = new SystemTransactionMethodCallList();
            crossChainMethodCallList.Add(nameof(CrossChainContract.Initialize), new AElf.Contracts.CrossChain.InitializeInput
            {
                ConsensusContractSystemName = ConsensusSmartContractAddressNameProvider.Name,
                TokenContractSystemName = TokenSmartContractAddressNameProvider.Name,
                ParentChainId = parentChainId
            });
            dto.InitializationSmartContracts.AddGenesisSmartContract<CrossChainContract>(
                CrossChainSmartContractAddressNameProvider.Name, crossChainMethodCallList);

            var osService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
            var that = this;
            AsyncHelper.RunSync(async () => { that.OsBlockchainNodeContext = await osService.StartAsync(dto); });
        }

        private SystemTransactionMethodCallList GenerateTokenInitializationCallList(Address issuer,
            List<string> tokenReceivers)
        {
//            const string symbol = "ELF";
//            tokenContractCallList.Add(nameof(TokenContract.CreateNativeToken), new CreateNativeTokenInput
//            {
//                Symbol = symbol,
//                Decimals = 2,
//                IsBurnable = true,
//                TokenName = "elf token",
//                TotalSupply = 10_0000_0000,
//                // Set the contract zero address as the issuer temporarily.
//                Issuer = issuer,
//                LockWhiteSystemContractNameList = {ConsensusSmartContractAddressNameProvider.Name}
//            });
//
//            tokenContractCallList.Add(nameof(TokenContract.IssueNativeToken), new IssueNativeTokenInput
//            {
//                Symbol = symbol,
//                Amount = 2_0000_0000,
//                ToSystemContractName = DividendsSmartContractAddressNameProvider.Name,
//                Memo = "Set dividends.",
//            });
            
            //TODO: Maybe should be removed after testing.
//            foreach (var tokenReceiver in tokenReceivers)
//            {
//                tokenContractCallList.Add(nameof(TokenContract.Issue), new IssueInput
//                {
//                    Symbol = symbol,
//                    Amount = 8_0000_0000 / tokenReceivers.Count,
//                    To = Address.FromPublicKey(ByteArrayHelpers.FromHexString(tokenReceiver)),
//                    Memo = "Set initial miner's balance.",
//                });
//            }

//            // Set fee pool address to dividend contract address.
//            tokenContractCallList.Add(nameof(TokenContract.SetFeePoolAddress),
//                DividendsSmartContractAddressNameProvider.Name);

            // Dont create and issue token on side chain.
            var tokenContractCallList = new SystemTransactionMethodCallList();
            tokenContractCallList.Add(nameof(TokenContract.InitializeWithContractSystemNames), new TokenContractInitializeInput
            {
                CrossChainContractSystemName = CrossChainSmartContractAddressNameProvider.Name
            });
            return tokenContractCallList;
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            var osService = context.ServiceProvider.GetService<IOsBlockchainNodeContextService>();
            var that = this;
            AsyncHelper.RunSync(() => osService.StopAsync(that.OsBlockchainNodeContext));
        }
    }
}