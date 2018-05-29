using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContracts.CSharpSmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;
using Xunit;
using AElf.Runtime.CSharp;
using Xunit.Frameworks.Autofac;
using AElf.Contracts;
using Path = AElf.Kernel.Path;

namespace AElf.Runtime.CSharp.Tests
{
    [UseAutofacTestFramework]
    public class RunnerTest
    {
        private IAccountDataProvider _dataProvider1;
        private IAccountDataProvider _dataProvider2;
        private SmartContractRunner _runner;

        private ISmartContractManager _smartContractManager;
        private IWorldStateManager _worldStateManager;
        private IChainCreationService _chainCreationService;
        private IBlockManager _blockManager;

        private ISmartContractRunnerFactory _smartContractRunnerFactory = new SmartContractRunnerFactory();
        private Hash ChainId1 { get; } = Hash.Generate();
        private Hash ChainId2 { get; } = Hash.Generate();

        public RunnerTest(SmartContractZero smartContractZero, IWorldStateManager worldStateManager,
                          IChainCreationService chainCreationService, IBlockManager blockManager, SmartContractStore smartContractStore)
        {
            _worldStateManager = worldStateManager;
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            _smartContractManager = new SmartContractManager(smartContractStore);
            _runner = new SmartContractRunner("../../../../AElf.Contracts.Examples/bin/Debug/netstandard2.0/");
        }

        public async Task Init()
        {
            var smartContractZero = typeof(Class1);
            var chain1 = await _chainCreationService.CreateNewChainAsync(ChainId1, smartContractZero);
            var genesis1 = await _blockManager.GetBlockAsync(chain1.GenesisBlockHash);
            _dataProvider1 = (await _worldStateManager.OfChain(ChainId1)).GetAccountDataProvider(Path.CalculatePointerForAccountZero(ChainId1));

            var chain2 = await _chainCreationService.CreateNewChainAsync(ChainId2, smartContractZero);
            var genesis2 = await _blockManager.GetBlockAsync(chain2.GenesisBlockHash);
            _dataProvider2 = (await _worldStateManager.OfChain(ChainId2)).GetAccountDataProvider(Path.CalculatePointerForAccountZero(ChainId2));
        }

        [Fact]
        public async Task Test()
        {
            Hash contractAddress1 = Hash.Generate();
            Hash contractAddress2 = Hash.Generate();
            await Init();
            byte[] code = null;
            using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath("../../../../AElf.Contracts.Examples/bin/Debug/netstandard2.0/AElf.Contracts.Examples.dll")))
            {
                code = file.ReadFully();
            }
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(code),
                ContractHash = Hash.Zero
            };

            await _smartContractManager.InsertAsync(contractAddress1, reg);
            await _smartContractManager.InsertAsync(contractAddress2, reg);
            _smartContractRunnerFactory.AddRunner(0, _runner);

            var chainContext = new ChainContext(new SmartContractZero(), Hash.Zero);

            var service = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, _worldStateManager);

            var executive1 = await service.GetExecutiveAsync(contractAddress1, chainContext.ChainId);
            executive1.SetSmartContractContext(new SmartContractContext()
            {
                ChainId = Hash.Zero,
                ContractAddress = contractAddress1,
                DataProvider = _dataProvider1.GetDataProvider(),
                SmartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, _worldStateManager)
            });

            var executive2 = await service.GetExecutiveAsync(contractAddress2, chainContext.ChainId);
            executive2.SetSmartContractContext(new SmartContractContext()
            {
                ChainId = Hash.Zero,
                ContractAddress = contractAddress2,
                DataProvider = _dataProvider2.GetDataProvider(),
                SmartContractService = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, _worldStateManager)
            });

            Hash sender = Hash.Generate();

            var init = new Transaction()
            {
                From = sender,
                MethodName = "InitializeAsync",
                Params = ByteString.CopyFrom(new Parameters().ToByteArray())
            };

            var transfer1 = new Transaction
            {
                From = Hash.Zero,
                MethodName = "Transfer",
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params = {
                                new Param
                                {
                                    StrVal = "0"
                                },
                                new Param
                                {
                                    StrVal = "1"
                                },
                                new Param
                                {
                                    LongVal = 10
                                }
                            }
                    }
                        .ToByteArray()
                )
            };

            var transfer2 = new Transaction
            {
                From = Hash.Zero,
                MethodName = "Transfer",
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params = {
                                new Param
                                {
                                    StrVal = "0"
                                },
                                new Param
                                {
                                    StrVal = "1"
                                },
                                new Param
                                {
                                    LongVal = 20
                                }
                            }
                    }
                        .ToByteArray()
                )
            };

            await executive1.SetTransactionContext(new TransactionContext()
            {
                Transaction = init,
                TransactionResult = new TransactionResult()
            }).Apply();

            await executive1.SetTransactionContext(new TransactionContext()
            {
                Transaction = transfer1,
                TransactionResult = new TransactionResult()
            }).Apply();


            await executive2.SetTransactionContext(new TransactionContext()
            {
                Transaction = init,
                TransactionResult = new TransactionResult()
            }).Apply();

            await executive2.SetTransactionContext(new TransactionContext()
            {
                Transaction = transfer2,
                TransactionResult = new TransactionResult()
            }).Apply();

            var getb0 = new Transaction
            {
                From = Hash.Zero,
                MethodName = "GetBalance",
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params = {
                                new Param
                                {
                                    StrVal = "0"
                                }
                            }
                    }
                        .ToByteArray()
                )
            };


            var getb1 = new Transaction
            {
                From = Hash.Zero,
                MethodName = "GetBalance",
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params = {
                                new Param
                                {
                                    StrVal = "1"
                                }
                            }
                    }
                        .ToByteArray()
                )
            };

            var bal10 = new TransactionResult();
            var bal20 = new TransactionResult();
            var bal11 = new TransactionResult();
            var bal21 = new TransactionResult();

            await executive1.SetTransactionContext(new TransactionContext()
            {
                Transaction = getb0,
                TransactionResult = bal10
            }).Apply();

            await executive2.SetTransactionContext(new TransactionContext()
            {
                Transaction = getb0,
                TransactionResult = bal20
            }).Apply();

            await executive1.SetTransactionContext(new TransactionContext()
            {
                Transaction = getb1,
                TransactionResult = bal11
            }).Apply();

            await executive2.SetTransactionContext(new TransactionContext()
            {
                Transaction = getb1,
                TransactionResult = bal21
            }).Apply();

            Assert.Equal((ulong)190, bal10.Logs.ToByteArray().ToUInt64());
            Assert.Equal((ulong)180, bal20.Logs.ToByteArray().ToUInt64());

            Assert.Equal((ulong)110, bal11.Logs.ToByteArray().ToUInt64());
            Assert.Equal((ulong)120, bal21.Logs.ToByteArray().ToUInt64());

        }
    }
}
