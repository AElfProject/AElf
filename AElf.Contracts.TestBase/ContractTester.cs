using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Authorization;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Dividends;
using AElf.Contracts.Genesis;
using AElf.Contracts.Resource;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using AElf.OS.Network;
using AElf.OS.Node.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Contracts.TestBase
{
    public class ContractTester<TContractTestAElfModule> : ITransientDependency
        where TContractTestAElfModule : ContractTestAElfModule
    {
        private IAbpApplicationWithInternalServiceProvider Application { get; set; } 

        public ECKeyPair KeyPair { get; set; }

        public ContractTester(int chainId = 0, ECKeyPair keyPair = null)
        {
            KeyPair = keyPair ?? CryptoHelpers.GenerateKeyPair();

            Application =
                AbpApplicationFactory.Create<TContractTestAElfModule>(options =>
                {
                    options.UseAutofac();
                    if (chainId != 0)
                    {
                        options.Services.Configure<ChainOptions>(o => { o.ChainId = chainId; });
                    }

                    if (keyPair != null)
                    {
                        options.Services.AddTransient(o =>
                        {
                            var mockService = new Mock<IAccountService>();
                            mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                                Task.FromResult(CryptoHelpers.SignWithPrivateKey(KeyPair.PrivateKey, data)));
                
                            mockService.Setup(a => a.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()
                            )).Returns<byte[], byte[], byte[]>((signature, data, publicKey) =>
                            {
                                var recoverResult = CryptoHelpers.RecoverPublicKey(signature, data, out var recoverPublicKey);
                                return Task.FromResult(recoverResult && publicKey.BytesEqual(recoverPublicKey));
                            });

                            mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(KeyPair.PublicKey);
                
                            return mockService.Object;
                        });
                    }
                });

            Application.Initialize();
        }

        private ContractTester(IAbpApplicationWithInternalServiceProvider application, ECKeyPair keyPair)
        {
            application.Services.AddTransient(o =>
            {
                var mockService = new Mock<IAccountService>();
                mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                    Task.FromResult(CryptoHelpers.SignWithPrivateKey(keyPair.PrivateKey, data)));
                
                mockService.Setup(a => a.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()
                )).Returns<byte[], byte[], byte[]>((signature, data, publicKey) =>
                {
                    var recoverResult = CryptoHelpers.RecoverPublicKey(signature, data, out var recoverPublicKey);
                    return Task.FromResult(recoverResult && publicKey.BytesEqual(recoverPublicKey));
                });

                mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(keyPair.PublicKey);
                
                return mockService.Object;
            });
            
            Application = application;
            
            KeyPair = keyPair;
        }
        
        private ContractTester(IAbpApplicationWithInternalServiceProvider application, int chainId)
        {
            application.Services.Configure<ChainOptions>(o => { o.ChainId = chainId; });
            
            Application = application;
        }
        
        /// <summary>
        /// Use default chain id.
        /// </summary>
        /// <param name="keyPair"></param>
        public ContractTester(ECKeyPair keyPair)
        {
            Application =
                AbpApplicationFactory.Create<TContractTestAElfModule>(options =>
                {
                    options.UseAutofac();
                    options.Services.AddTransient(o =>
                    {
                        var mockService = new Mock<IAccountService>();
                        mockService.Setup(a => a.SignAsync(It.IsAny<byte[]>())).Returns<byte[]>(data =>
                            Task.FromResult(CryptoHelpers.SignWithPrivateKey(keyPair.PrivateKey, data)));
                
                        mockService.Setup(a => a.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()
                        )).Returns<byte[], byte[], byte[]>((signature, data, publicKey) =>
                        {
                            var recoverResult = CryptoHelpers.RecoverPublicKey(signature, data, out var recoverPublicKey);
                            return Task.FromResult(recoverResult && publicKey.BytesEqual(recoverPublicKey));
                        });

                        mockService.Setup(a => a.GetPublicKeyAsync()).ReturnsAsync(keyPair.PublicKey);
                
                        return mockService.Object;
                    });
                });
            
            Application.Initialize();

            KeyPair = keyPair;
        }
        
        /// <summary>
        /// Use randomized ECKeyPair.
        /// </summary>
        /// <param name="chainId"></param>
        public ContractTester(int chainId)
        {
            Application =
                AbpApplicationFactory.Create<TContractTestAElfModule>(options =>
                {
                    options.UseAutofac();
                    options.Services.Configure<ChainOptions>(o => { o.ChainId = chainId; });
                });
            Application.Initialize();
        }

        public int GetChainId()
        {
            var chainManager = Application.ServiceProvider.GetRequiredService<IChainManager>();
            return chainManager.GetChainId();
        }

        public ContractTester<TContractTestAElfModule> CreateNewContractTester(ECKeyPair keyPair)
        {
            return new ContractTester<TContractTestAElfModule>(Application, keyPair);
        }
        
        public ContractTester<TContractTestAElfModule> CreateNewContractTester(int chainId)
        {
            return new ContractTester<TContractTestAElfModule>(Application, chainId);
        }

        public async Task<byte[]> GetPublicKeyAsync()
        {
            var accountService = Application.ServiceProvider.GetRequiredService<IAccountService>();
            return await accountService.GetPublicKeyAsync();
        }
        
        public Address GetContractAddress(Hash name)
        {
            var smartContractAddressService =
                Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();
            return name == Hash.FromString(typeof(BasicContractZero).FullName)
                ? smartContractAddressService.GetZeroSmartContractAddress()
                : smartContractAddressService.GetAddressByContractName(name);
        }

        public Address GetContractAddress(Type contractType)
        {
            var smartContractAddressService =
                Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();
            return contractType == typeof(BasicContractZero)
                ? smartContractAddressService.GetZeroSmartContractAddress()
                : smartContractAddressService.GetAddressByContractName(Hash.FromString(contractType.FullName));
        }

        public Address GetZeroContractAddress()
        {
            var smartContractAddressService =
                Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();
            return smartContractAddressService.GetZeroSmartContractAddress();
        }

        public Address GetConsensusContractAddress()
        {
            var smartContractAddressService =
                Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();
            return smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                .Name);
        }

        public Address GetCallOwnerAddress()
        {
            return Address.FromPublicKey(KeyPair.PublicKey);
        }

        /// <summary>
        /// Initial a chain with given chain id (passed to ctor),
        /// and produce the genesis block with provided contract types.
        /// </summary>
        /// <param name="contractTypes"></param>
        /// <returns>Return contract addresses as the param order.</returns>
        public async Task InitialChainAsync(params Type[] contractTypes)
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
            dto.InitializationSmartContracts.AddGenesisSmartContracts(contractTypes);

            await osBlockchainNodeContextService.StartAsync(dto);
        }

        /// <summary>
        /// Generate a transaction and sign it.
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="methodName"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        public async Task<Transaction> GenerateTransaction(Address contractAddress, string methodName, params object[] objects)
        {
            var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var refBlock = await blockchainService.GetBestChainLastBlock();
            var tx = new Transaction
            {
                From = Address.FromPublicKey(KeyPair.PublicKey),
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects)),
                RefBlockNumber = refBlock.Height,
                RefBlockPrefix = ByteString.CopyFrom(refBlock.GetHash().Value.Take(4).ToArray())
            };

            var signature = CryptoHelpers.SignWithPrivateKey(KeyPair.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            return tx;
        }

        /// <summary>
        /// Generate a transaction and sign it by provided key pair.
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="methodName"></param>
        /// <param name="ecKeyPair"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        public async Task<Transaction> GenerateTransaction(Address contractAddress, string methodName, ECKeyPair ecKeyPair,
            params object[] objects)
        {
            var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var refBlock = await blockchainService.GetBestChainLastBlock();
            var tx = new Transaction
            {
                From = Address.FromPublicKey(KeyPair.PublicKey),
                To = contractAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(objects)),
                RefBlockNumber = refBlock.Height,
                RefBlockPrefix = ByteString.CopyFrom(refBlock.GetHash().Value.Take(4).ToArray())
            };

            var signature = CryptoHelpers.SignWithPrivateKey(ecKeyPair.PrivateKey, tx.GetHash().DumpByteArray());
            tx.Sigs.Add(ByteString.CopyFrom(signature));

            return tx;
        }

        /// <summary>
        /// Mine a block with given normal txs and system txs.
        /// Normal txs will use tx pool while system txs not.
        /// </summary>
        /// <param name="txs"></param>
        /// <param name="systemTxs"></param>
        /// <returns></returns>
        public async Task<Block> MineAsync(List<Transaction> txs)
        {
            await AddTransactions(txs);
            var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var preBlock = await blockchainService.GetBestChainLastBlock();
            var minerService = Application.ServiceProvider.GetRequiredService<IMinerService>();

            return await minerService.MineAsync(preBlock.GetHash(), preBlock.Height,
                DateTime.UtcNow.AddMilliseconds(4000));
        }

        /// <summary>
        /// In test cases, we can't distinguish normal txs and system txs,
        /// so just keep in mind if some txs looks like system txs,
        /// just add them first manually.
        /// </summary>
        /// <param name="txs"></param>
        /// <returns></returns>
        private async Task AddTransactions(IEnumerable<Transaction> txs)
        {
            var txHub = Application.ServiceProvider.GetRequiredService<ITxHub>();
            await txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
            {
                Transactions = txs
            });
        }

        /// <summary>
        /// Generate a tx then package the new tx to a new block.
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="methodName"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        public async Task<TransactionResult> ExecuteContractWithMiningAsync(Address contractAddress, string methodName,
            params object[] objects)
        {
            var tx = await GenerateTransaction(contractAddress, methodName, objects);
            await MineAsync(new List<Transaction> {tx});
            var result = await GetTransactionResult(tx.GetHash());

            return result;
        }

        /// <summary>
        /// Using tx to call a method without mining.
        /// The state database won't change.
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="methodName"></param>
        /// <param name="objects"></param>
        /// <returns></returns>
        public async Task<ByteString> CallContractMethodAsync(Address contractAddress, string methodName,
            params object[] objects)
        {
            var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var transactionReadOnlyExecutionService =
                Application.ServiceProvider.GetRequiredService<ITransactionReadOnlyExecutionService>();
            var tx = await GenerateTransaction(contractAddress, methodName, objects);
            var preBlock = await blockchainService.GetBestChainLastBlock();
            var transactionTrace = await transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
                {
                    BlockHash = preBlock.GetHash(),
                    BlockHeight = preBlock.Height
                },
                tx,
                DateTime.UtcNow);

            return transactionTrace.RetVal?.Data;
        }

        public void SignTransaction(ref List<Transaction> transactions, ECKeyPair callerKeyPair)
        {
            foreach (var transaction in transactions)
            {
                var signature =
                    CryptoHelpers.SignWithPrivateKey(callerKeyPair.PrivateKey, transaction.GetHash().DumpByteArray());
                transaction.Sigs.Add(ByteString.CopyFrom(signature));
            }
        }
        
        public void SupplyTransactionParameters(ref List<Transaction> transactions)
        {
            var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var refBlock = AsyncHelper.RunSync(() => blockchainService.GetBestChainLastBlock());
            foreach (var transaction in transactions)
            {
                transaction.RefBlockNumber = refBlock.Height;
                transaction.RefBlockPrefix = ByteString.CopyFrom(refBlock.GetHash().Value.Take(4).ToArray());
            }
        }

        public async Task<Chain> GetChainAsync()
        {
            var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            return await blockchainService.GetChainAsync();
        }

        /// <summary>
        /// Execute a block and add it to chain database.
        /// </summary>
        /// <param name="block"></param>
        /// <param name="txs"></param>
        /// <returns></returns>
        public async Task ExecuteBlock(Block block, List<Transaction> txs)
        {
            var blockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
            var transactionManager = Application.ServiceProvider.GetRequiredService<ITransactionManager>();
            var blockchainExecutingService =
                Application.ServiceProvider.GetRequiredService<IBlockchainExecutingService>();
            txs.ForEach(tx => AsyncHelper.RunSync(() => transactionManager.AddTransactionAsync(tx)));
            await blockchainService.AddBlockAsync(block);
            var chain = await blockchainService.GetChainAsync();
            var status = await blockchainService.AttachBlockToChainAsync(chain, block);
            await blockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
        }

        /// <summary>
        /// Get the execution result of a tx by its tx id.
        /// </summary>
        /// <param name="txId"></param>
        /// <returns></returns>
        public async Task<TransactionResult> GetTransactionResult(Hash txId)
        {
            var transactionResultQueryService =
                Application.ServiceProvider.GetRequiredService<ITransactionResultQueryService>();
            return await transactionResultQueryService.GetTransactionResultAsync(txId);
        }

        public Address GetAddress(ECKeyPair keyPair)
        {
            return Address.FromPublicKey(keyPair.PublicKey);
        }

        /// <summary>
        /// Zero Contract and Consensus Contract will deploy independently, thus this list won't contain this two contracts.
        /// </summary>
        /// <returns></returns>
        public List<Type> GetDefaultContractTypes()
        {
            return new List<Type>
            {
                typeof(TokenContract),
                typeof(CrossChainContract),
                typeof(AuthorizationContract),
                typeof(ResourceContract),
                typeof(DividendsContract),
                typeof(FeeReceiverContract)
            };
        }
    }
}