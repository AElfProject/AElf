using System;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
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
using AElf.Kernel.Concurrency.Execution;
using Xunit.Frameworks.Autofac;
using Path = AElf.Kernel.Path;
using AElf.Kernel.Tests.Concurrency.Scheduling;

namespace AElf.Kernel.Tests.Concurrency.Execution
{
    public class MockSetup
    {
        // IncrementId is used to differentiate txn
        // which is identified by From/To/IncrementId
        private static int _incrementId = 0;
        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong) n;
        }

        public Hash ChainId1 { get; } = Hash.Generate();
        public Hash ChainId2 { get; } = Hash.Generate();
        public ISmartContractManager SmartContractManager;
        public ISmartContractService SmartContractService;

        public IChainContextService ChainContextService;

        public IAccountDataProvider DataProvider1;
        public IAccountDataProvider DataProvider2;

        public Hash SampleContractAddress1 { get; } = Hash.Generate();
        public Hash SampleContractAddress2 { get; } = Hash.Generate();

        public IExecutive Executive1 { get; private set; }
        public IExecutive Executive2 { get; private set; }

        public ServicePack ServicePack;

        private IWorldStateManager _worldStateManager;
        private IChainCreationService _chainCreationService;
        private IBlockManager _blockManager;

        private ISmartContractRunnerFactory _smartContractRunnerFactory = new SmartContractRunnerFactory();

        public MockSetup(IWorldStateManager worldStateManager, IChainCreationService chainCreationService, IBlockManager blockManager, SmartContractStore smartContractStore, IChainContextService chainContextService)
        {
            _worldStateManager = worldStateManager;
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            ChainContextService = chainContextService;
            SmartContractManager = new SmartContractManager(smartContractStore);
            var runner = new SmartContractRunner("../../../../AElf.Contracts.Examples/bin/Debug/netstandard2.0/");
            _smartContractRunnerFactory.AddRunner(0, runner);
            Task.Factory.StartNew(async () =>
            {
                await Init();
            }).Unwrap().Wait();
            SmartContractService = new SmartContractService(SmartContractManager, _smartContractRunnerFactory, _worldStateManager);
            Task.Factory.StartNew(async () =>
            {
                await DeploySampleContracts();
            }).Unwrap().Wait();
            ServicePack = new ServicePack()
            {
                ChainContextService = chainContextService,
                SmartContractService = SmartContractService,
                ResourceDetectionService = new NewMockResourceUsageDetectionService()
            };
        }

        private async Task Init()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(new byte[] { }),
                ContractHash = Hash.Zero
            };
            var chain1 = await _chainCreationService.CreateNewChainAsync(ChainId1, reg);
            var genesis1 = await _blockManager.GetBlockAsync(chain1.GenesisBlockHash);
            DataProvider1 = (await _worldStateManager.OfChain(ChainId1)).GetAccountDataProvider(Path.CalculatePointerForAccountZero(ChainId1));

            var chain2 = await _chainCreationService.CreateNewChainAsync(ChainId2, reg);
            var genesis2 = await _blockManager.GetBlockAsync(chain2.GenesisBlockHash);
            DataProvider2 = (await _worldStateManager.OfChain(ChainId2)).GetAccountDataProvider(Path.CalculatePointerForAccountZero(ChainId2));
        }

        private async Task DeploySampleContracts()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(ExampleContractCode),
                ContractHash = Hash.Zero
            };

            await SmartContractService.DeployContractAsync(SampleContractAddress1, reg);
            await SmartContractService.DeployContractAsync(SampleContractAddress2, reg);
            Executive1 = await SmartContractService.GetExecutiveAsync(SampleContractAddress1, ChainId1);
            Executive2 = await SmartContractService.GetExecutiveAsync(SampleContractAddress2, ChainId2);
        }

        public byte[] ExampleContractCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath("../../../../AElf.Contracts.Examples/bin/Debug/netstandard2.0/AElf.Contracts.Examples.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }

        public void Initialize1(Hash account, ulong qty)
        {
            Executive1.SetTransactionContext(GetInitializeTxnCtxt(SampleContractAddress1, account, qty)).Apply().Wait();
        }

        public void Initialize2(Hash account, ulong qty)
        {
            Executive2.SetTransactionContext(GetInitializeTxnCtxt(SampleContractAddress2, account, qty)).Apply().Wait();
        }

        private TransactionContext GetInitializeTxnCtxt(Hash contractAddress, Hash account, ulong qty)
        {
            var tx = new Transaction
            {
                From = Hash.Zero,
                To = contractAddress,
                IncrementId = NewIncrementId(),
                MethodName = "InitializeAsync",
                Params = ByteString.CopyFrom(new Parameters()
                {
                    Params = {
                                new Param
                                {
                                    HashVal = account
                                },
                                new Param
                                {
                                    UlongVal = qty
                                }
                            }
                }.ToByteArray())
            };
            return new TransactionContext()
            {
                Transaction = tx
            };
        }

        public Transaction GetTransferTxn1(Hash from, Hash to, ulong qty)
        {
            return GetTransferTxn(SampleContractAddress1, from, to, qty);
        }

        public Transaction GetTransferTxn2(Hash from, Hash to, ulong qty)
        {
            return GetTransferTxn(SampleContractAddress2, from, to, qty);
        }

        private Transaction GetTransferTxn(Hash contractAddress, Hash from, Hash to, ulong qty)
        {
            // TODO: Test with IncrementId
            return new Transaction
            {
                From = from,
                To = contractAddress,
                IncrementId = NewIncrementId(),
                MethodName = "Transfer",
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params = {
                                new Param
                                {
                                    HashVal = from
                                },
                                new Param
                                {
                                    HashVal = to
                                },
                                new Param
                                {
                                    UlongVal = qty
                                }
                            }
                    }.ToByteArray()
                )
            };
        }

        private Transaction GetBalanceTxn(Hash contractAddress, Hash account)
        {
            return new Transaction
            {
                From = Hash.Zero,
                To = contractAddress,
                MethodName = "GetBalance",
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params = {
                                new Param
                                {
                                    HashVal = account
                                }
                            }
                    }.ToByteArray()
                )
            };
        }

        public ulong GetBalance1(Hash account)
        {
            var txn = GetBalanceTxn(SampleContractAddress1, account);
            var txnCtxt = new TransactionContext()
            {
                Transaction = txn
            };

            Executive1.SetTransactionContext(txnCtxt).Apply().Wait();

            return txnCtxt.Trace.RetVal.Unpack<UInt64Value>().Value;
        }

        public ulong GetBalance2(Hash account)
        {
            var txn = GetBalanceTxn(SampleContractAddress2, account);
            var txnRes = new TransactionResult();
            var txnCtxt = new TransactionContext()
            {
                Transaction = txn
            };
            Executive2.SetTransactionContext(txnCtxt).Apply().Wait();

            return txnCtxt.Trace.RetVal.Unpack<UInt64Value>().Value;
        }

        private Transaction GetSTTxn(Hash contractAddress, Hash transactionHash)
        {
            return new Transaction
            {
                From = Hash.Zero,
                To = contractAddress,
                MethodName = "GetTransactionStartTime",
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params = {
                                new Param
                                {
                                    HashVal = transactionHash
                                }
                            }
                    }.ToByteArray()
                )
            };
        }

        private Transaction GetETTxn(Hash contractAddress, Hash transactionHash)
        {
            return new Transaction
            {
                From = Hash.Zero,
                To = contractAddress,
                MethodName = "GetTransactionEndTime",
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params = {
                                new Param
                                {
                                    HashVal = transactionHash
                                }
                            }
                    }.ToByteArray()
                )
            };
        }

        public DateTime GetTransactionStartTime1(ITransaction tx)
        {
            var txn = GetSTTxn(SampleContractAddress1, tx.GetHash());
            var txnCtxt = new TransactionContext()
            {
                Transaction = txn
            };

            Executive1.SetTransactionContext(txnCtxt).Apply().Wait();

            var dtStr = Encoding.UTF8.GetString(txnCtxt.Trace.RetVal.Unpack<BytesValue>().Value.ToByteArray());
            //var dtStr = BitConverter.ToString(txnCtxt.Trace.RetVal.Unpack<BytesValue>().Value.ToByteArray()).Replace("-", "");

            return DateTime.ParseExact(dtStr, "yyyy-MM-dd HH:mm:ss.ffffff", null);
        }

        public DateTime GetTransactionEndTime1(ITransaction tx)
        {
            var txn = GetETTxn(SampleContractAddress1, tx.GetHash());
            var txnCtxt = new TransactionContext()
            {
                Transaction = txn
            };

            Executive1.SetTransactionContext(txnCtxt).Apply().Wait();

            var dtStr = Encoding.UTF8.GetString(txnCtxt.Trace.RetVal.Unpack<BytesValue>().Value.ToByteArray());

            return DateTime.ParseExact(dtStr, "yyyy-MM-dd HH:mm:ss.ffffff", null);
        }
    }
}
