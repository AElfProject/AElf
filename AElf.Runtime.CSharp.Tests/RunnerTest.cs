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
            Hash account0 = Hash.Generate();
            Hash account1 = Hash.Generate();
            Hash contractAddress1 = _mock.SampleContractAddress1;
            Hash contractAddress2 = _mock.SampleContractAddress2;

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

            var init0 = new Transaction()
            {
                From = sender,
                To = Hash.Zero,
                MethodName = "InitializeAsync",
                Params = ByteString.CopyFrom(new Parameters()
                {
                    Params = {
                                new Param
                                {
                                    HashVal = account0
                                },
                                new Param
                                {
                                    LongVal = 200
                                }
                            }
                }.ToByteArray())
            };

            var init1 = new Transaction()
            {
                From = sender,
                To = Hash.Zero,
                MethodName = "InitializeAsync",
                Params = ByteString.CopyFrom(new Parameters()
                {
                    Params = {
                                new Param
                                {
                                    HashVal = account1
                                },
                                new Param
                                {
                                    LongVal = 100
                                }
                            }
                }.ToByteArray())
            };

            var transfer1 = new Transaction
            {
                From = Hash.Zero,
                To = Hash.Zero,
                MethodName = "Transfer",
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params = {
                                new Param
                                {
                                    HashVal = account0
                                },
                                new Param
                                {
                                    HashVal = account1
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
                To = Hash.Zero,
                MethodName = "Transfer",
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params = {
                                new Param
                                {
                                    HashVal = account0
                                },
                                new Param
                                {
                                    HashVal = account1
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
                Transaction = init0
            }).Apply();

            await executive1.SetTransactionContext(new TransactionContext()
            {
                Transaction = init1
            }).Apply();

            await executive1.SetTransactionContext(new TransactionContext()
            {
                Transaction = transfer1
            }).Apply();


            await executive2.SetTransactionContext(new TransactionContext()
            {
                Transaction = init0
            }).Apply();

            await executive2.SetTransactionContext(new TransactionContext()
            {
                Transaction = init1
            }).Apply();

            await executive2.SetTransactionContext(new TransactionContext()
            {
                Transaction = transfer2
            }).Apply();

            var getb0 = new Transaction
            {
                From = Hash.Zero,
                To = Hash.Zero,
                MethodName = "GetBalance",
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params = {
                                new Param
                                {
                                    HashVal = account0
                                }
                            }
                    }
                        .ToByteArray()
                )
            };

            var getb1 = new Transaction
            {
                From = Hash.Zero,
                To = Hash.Zero,
                MethodName = "GetBalance",
                Params = ByteString.CopyFrom(
                    new Parameters
                    {
                        Params = {
                                new Param
                                {
                                    HashVal = account1
                                }
                            }
                    }
                        .ToByteArray()
                )
            };

            var tc10 = new TransactionContext(){
                Transaction = getb0  
            };
            var tc20 = new TransactionContext(){
                Transaction = getb0  
            };
            var tc11 = new TransactionContext(){
                Transaction = getb1
            };
            var tc21 = new TransactionContext(){
                Transaction = getb1  
            };
            await executive1.SetTransactionContext(tc10).Apply();

            await executive2.SetTransactionContext(tc20).Apply();

            await executive1.SetTransactionContext(tc11).Apply();

            await executive2.SetTransactionContext(tc21).Apply();

            Assert.Equal((ulong)190, tc10.Trace.RetVal.Unpack<UInt64Value>().Value);
            Assert.Equal((ulong)180, tc20.Trace.RetVal.Unpack<UInt64Value>().Value);

            Assert.Equal((ulong)110, tc11.Trace.RetVal.Unpack<UInt64Value>().Value);
            Assert.Equal((ulong)120, tc21.Trace.RetVal.Unpack<UInt64Value>().Value);

        }
    }
}
