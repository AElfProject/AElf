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
        private MockSetup _mock;
        private ISmartContractService _service;
        public RunnerTest(MockSetup mock)
        {
            _mock = mock;
            _service = mock.SmartContractService;
        }

        [Fact]
        public async Task Test()
        {
            Hash contractAddress1 = Hash.Generate();
            Hash contractAddress2 = Hash.Generate();

            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(_mock.ExampleContractCode),
                ContractHash = Hash.Zero
            };

            await _service.DeployContractAsync(contractAddress1, reg);
            await _service.DeployContractAsync(contractAddress2, reg);

            var chainContext = new ChainContext()
            {
                ChainId=Hash.Zero
            };

            //var service = new SmartContractService(_smartContractManager, _smartContractRunnerFactory, _worldStateManager);

            var executive1 = await _service.GetExecutiveAsync(contractAddress1, _mock.ChainId1);
            executive1.SetSmartContractContext(new SmartContractContext()
            {
                ChainId = Hash.Zero,
                ContractAddress = contractAddress1,
                DataProvider = _mock.DataProvider1.GetDataProvider(),
                SmartContractService = _service
            });

            var executive2 = await _service.GetExecutiveAsync(contractAddress2, _mock.ChainId2);
            executive2.SetSmartContractContext(new SmartContractContext()
            {
                ChainId = Hash.Zero,
                ContractAddress = contractAddress2,
                DataProvider = _mock.DataProvider2.GetDataProvider(),
                SmartContractService = _service
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
