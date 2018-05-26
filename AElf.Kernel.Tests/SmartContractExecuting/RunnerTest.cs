using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using AElf.Kernel;
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
using Path = AElf.Kernel.Path;

namespace AElf.Runtime.CSharp.Tests
{
    [UseAutofacTestFramework]
    public class RunnerTest
    {
        private IAccountDataProvider _dataProvider1;
        private IAccountDataProvider _dataProvider2;
        private CSharpAssemblyLoadContext _loadContext1;
        private BinaryCSharpSmartContractRunner _runner1;
        
        private CSharpAssemblyLoadContext _loadContext2;
        private BinaryCSharpSmartContractRunner _runner2;
        
        private IWorldStateManager _worldStateManager;
        private IChainCreationService _chainCreationService;
        private IBlockManager _blockManager;

        private Hash ChainId1 { get; } = Hash.Generate();
        private Hash ChainId2 { get; } = Hash.Generate();
        
        public RunnerTest(SmartContractZero smartContractZero, IWorldStateManager worldStateManager,
            IChainCreationService chainCreationService, IBlockManager blockManager)
        {
            _worldStateManager = worldStateManager;
            _chainCreationService = chainCreationService;
            _blockManager = blockManager;
            
            _loadContext1 = new CSharpAssemblyLoadContext(
                System.IO.Path.GetFullPath("../../../../AElf.Contracts.Examples"),
                AppDomain.CurrentDomain.GetAssemblies());
            _loadContext2 = new CSharpAssemblyLoadContext(
                System.IO.Path.GetFullPath("../../../../AElf.Contracts.Examples"),
                AppDomain.CurrentDomain.GetAssemblies());
            _runner1 = new BinaryCSharpSmartContractRunner(smartContractZero, _loadContext1);
            _runner2 = new BinaryCSharpSmartContractRunner(smartContractZero, _loadContext2);
        }

        public async Task init()
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
            await init();
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
            
            var dep = new SmartContractDeployment
            {
                ContractHash = Hash.Zero,
                Caller = Hash.Zero,
                ConstructParams = ByteString.CopyFrom(
                    new Parameters().ToByteArray()
                ),
                IncrementId = 1
            };

            
            Assert.Equal("", string.Join("\n", AppDomain.CurrentDomain.GetAssemblies().Where(x=>x.GetName().Name.Contains("Api")).Select(x=>x.GetName().Name.ToString())));
            
            var contract1 = await _runner1.RunAsync(reg, dep, _dataProvider1);
            var contract2 = await _runner2.RunAsync(reg, dep, _dataProvider2);
            await contract1.InitializeAsync(_dataProvider1);
            await contract2.InitializeAsync(_dataProvider2);
            
            var transfer1 = new SmartContractInvokeContext 
            {
                MethodName = "Transfer",
                Caller = Hash.Zero,
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
            
            var transfer2 = new SmartContractInvokeContext 
            {
                MethodName = "Transfer",
                Caller = Hash.Zero,
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
            
            await contract1.InvokeAsync(transfer1);
            await contract2.InvokeAsync(transfer2);

            var b0 = new SmartContractInvokeContext 
            {
                MethodName = "GetBalance",
                Caller = Hash.Zero,
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

            
            var b1 = new SmartContractInvokeContext 
            {
                MethodName = "GetBalance",
                Caller = Hash.Zero,
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
            
            var bal10 = await contract1.InvokeAsync(b0);
            
            var bal20 = await contract2.InvokeAsync(b0);
            var bal11 = await contract1.InvokeAsync(b1);
            
            var bal21 = await contract2.InvokeAsync(b1);
            
            //            Assert.Equal(bal10, bal20);
            Assert.Equal((ulong) 190, bal10);
            Assert.Equal((ulong) 180, bal20);
            
            Assert.Equal((ulong) 110, bal11);
            Assert.Equal((ulong) 120, bal21);
            
        }
    }
}