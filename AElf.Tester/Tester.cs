using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Dividends;
using AElf.Contracts.Genesis;
using AElf.Contracts.Resource;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract;
using AElf.OS.Network;
using AElf.OS.Node.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.Threading;

namespace AElf.Tester
{
    public class Tester
    {
        private IAbpApplicationWithInternalServiceProvider Application { get; set; }

        private List<Type> DefaultSmartContractsTypes { get; set; } = new List<Type>
        {
            typeof(TokenContract),
            typeof(CrossChainContract),
            typeof(ResourceContract),
            typeof(DividendsContract),
            typeof(FeeReceiverContract)
        };
        
        public ECKeyPair KeyPair { get; }

        public Chain Chain
        {
            get
            {
                var chainManager = Application.ServiceProvider.GetRequiredService<IChainManager>();
                return AsyncHelper.RunSync(() => chainManager.GetAsync());
            }
        }

        public Tester(ECKeyPair keyPair = null, int port = 0, int chainId = 0)
        {
            KeyPair = keyPair ?? CryptoHelpers.GenerateKeyPair();

            Application =
                AbpApplicationFactory.Create<TesterAElfModule>(options =>
                {
                    options.UseAutofac();
                    
                    options.Services.AddTransient(o =>
                    {
                        var mockService = new Mock<IAccountService>();
                        mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                            Task.FromResult(CryptoHelpers.SignWithPrivateKey(KeyPair.PrivateKey, data)));

                        mockService.Setup(a => a.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(),
                            It.IsAny<byte[]>()
                        )).Returns<byte[], byte[], byte[]>((signature, data, publicKey) =>
                        {
                            var recoverResult =
                                CryptoHelpers.RecoverPublicKey(signature, data, out var recoverPublicKey);
                            return Task.FromResult(recoverResult && publicKey.BytesEqual(recoverPublicKey));
                        });

                        mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(KeyPair.PublicKey);

                        return mockService.Object;
                    });

                    if (port != 0)
                    {
                        options.Services.Configure<NetworkOptions>(o => o.ListeningPort = port);
                    }

                    if (chainId == 0)
                    {
                        chainId = ChainHelpers.ConvertBase58ToChainId("AELF");
                    }
                    options.Services.Configure<ChainOptions>(o => { o.ChainId = chainId; });

                });
            
            Application.Initialize();
        }

        public void SetDefaultSmartContracts(List<Type> smartContractsTypes)
        {
            DefaultSmartContractsTypes = smartContractsTypes;
        }
        
        public async Task StartAsync()
        {
            var osBlockchainNodeContextService =
                Application.ServiceProvider.GetRequiredService<IOsBlockchainNodeContextService>();
            
            var chainOptions = Application.ServiceProvider.GetService<IOptionsSnapshot<ChainOptions>>().Value;
            var dto = new OsBlockchainNodeContextStartDto
            {
                ChainId = chainOptions.ChainId,
                ZeroSmartContract = typeof(BasicContractZero)
            };

            dto.InitializationSmartContracts.AddConsensusSmartContract<ConsensusContract>();
            dto.InitializationSmartContracts.AddGenesisSmartContract<ConsensusContract>(ConsensusSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract<TokenContract>(TokenSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract<DividendsContract>(DividendsSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract<ResourceContract>(ResourceSmartContractAddressNameProvider.Name);
            dto.InitializationSmartContracts.AddGenesisSmartContract<FeeReceiverContract>(ResourceFeeReceiverSmartContractAddressNameProvider.Name);

            await osBlockchainNodeContextService.StartAsync(dto);
        }
    }
}