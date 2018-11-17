using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Common.Extensions;
using AElf.Kernel.Storages;
using AElf.SmartContract;
using AElf.Kernel.Tests.Concurrency.Scheduling;
using AElf.Sdk.CSharp;
using AElf.Types.CSharp.MetadataAttribute;
using Akka.Routing;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.Concurrency.Metadata
{
    [UseAutofacTestFramework]
    public class FunctionMetadataServiceTest
    {
        private IDataStore _dataStore;
        private readonly ISmartContractRunnerFactory _smartContractRunnerFactory;
        private readonly IFunctionMetadataService _functionMetadataService;

        public FunctionMetadataServiceTest(IFunctionMetadataService functionMetadataService, IDataStore dataStore, ISmartContractRunnerFactory smartContractRunnerFactory)
        {
            _functionMetadataService = functionMetadataService;
            _dataStore = dataStore;
            _smartContractRunnerFactory = smartContractRunnerFactory;
        }

        [Fact]
        public async Task TestDepolyContract()
        {   
            var chainId = Hash.Generate();
            var runner = _smartContractRunnerFactory.GetRunner(0);
            var contractCType = typeof(TestContractC);
            var contractBType = typeof(TestContractB);
            var contractAType = typeof(TestContractA);
            
            var contractCTemplate = runner.ExtractMetadata(contractCType);
            var contractBTemplate = runner.ExtractMetadata(contractBType);
            var contractATemplate = runner.ExtractMetadata(contractAType);
            
            var addrA = Address.FromString("TestContractA");
            var addrB = Address.FromString("TestContractB");
            var addrC = Address.FromString("TestContractC");
            Console.WriteLine(addrC);

            await _functionMetadataService.DeployContract(chainId, addrC, contractCTemplate);

            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrC.DumpHex() + ".Func0")));

            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing) 
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrC.DumpHex() + ".Func1")));
            
            await _functionMetadataService.DeployContract(chainId, addrB, contractBTemplate);
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrC.DumpHex() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrB.DumpHex() + ".Func0")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrB.DumpHex() + ".Func1")));

            await _functionMetadataService.DeployContract(chainId, addrA, contractATemplate);
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>()), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.DumpHex() + ".Func0(int)")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.DumpHex() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.DumpHex() + ".Func0")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.DumpHex() + ".Func2"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.DumpHex() + ".Func1")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.DumpHex() + ".Func2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.DumpHex() + ".Func0",
                    addrB.DumpHex() + ".Func0", 
                    addrC.DumpHex() + ".Func0"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.DumpHex() + ".Func3")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.DumpHex() + ".Func2"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.DumpHex() + ".Func4")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.DumpHex() + ".Func3",
                    addrB.DumpHex() + ".Func1"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.DumpHex() + ".Func5")));

            var callGraph = new SerializedCallGraph
            {
                Vertices =
                {
                    addrC.DumpHex() + ".Func0",
                    addrC.DumpHex() + ".Func1",
                    addrB.DumpHex() + ".Func0",
                    addrB.DumpHex() + ".Func1",
                    addrA.DumpHex() + ".Func0(int)",
                    addrA.DumpHex() + ".Func0",
                    addrA.DumpHex() + ".Func1",
                    addrA.DumpHex() + ".Func2",
                    addrA.DumpHex() + ".Func3",
                    addrA.DumpHex() + ".Func4",
                    addrA.DumpHex() + ".Func5"
                },
                Edges =
                {
                    new GraphEdge
                    {
                        Source = addrB.DumpHex() + ".Func0",
                        Target = addrC.DumpHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func0",
                        Target = addrA.DumpHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func1",
                        Target = addrA.DumpHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func3",
                        Target = addrB.DumpHex() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func3",
                        Target = addrA.DumpHex() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func3",
                        Target = addrC.DumpHex() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func4",
                        Target = addrA.DumpHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func5",
                        Target = addrB.DumpHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func5",
                        Target = addrA.DumpHex() + ".Func3"
                    }
                }
            };
            Assert.Equal(callGraph, await _dataStore.GetAsync<SerializedCallGraph>(chainId.OfType(HashType.CallingGraph)));
        }
        [Fact]
        public async Task TestEmptyContract()
        {
            var chainId = Hash.Generate();
            var runner = _smartContractRunnerFactory.GetRunner(0);
            var contractCType = typeof(TestContractC);
            var contractBType = typeof(TestContractB);
            var contractAType = typeof(TestContractA);
            var nonAttrContract1Type = typeof(TestNonAttrContract1);
            var nonAttrContract2Type = typeof(TestNonAttrContract2);
            var refNonAttrContractType = typeof(TestRefNonAttrContract);
            
            var contractCTemplate = runner.ExtractMetadata(contractCType);
            var contractBTemplate = runner.ExtractMetadata(contractBType);
            var contractATemplate = runner.ExtractMetadata(contractAType);
            var nonAttrContract1Template = runner.ExtractMetadata(nonAttrContract1Type);
            var nonAttrContract2Template = runner.ExtractMetadata(nonAttrContract2Type);
            var refNonAttrContractTemplate = runner.ExtractMetadata(refNonAttrContractType);
            
            
            var contract1Addr = Address.FromString("TestNonAttrContract1"); // 0x3f77405cbfe1e48e2fa0e4bf6a4e5917f768
            var contract2Addr = Address.FromString("TestNonAttrContract2"); // 0xb4e0cc36ede5d518fbabd1ed5498093e4b71
            var refContractAddr = Address.FromString("TestRefNonAttrContract"); // 0x7c7f78ecc9f78be2a502e5bf9f22112c6a47
            
            var addrA = Address.FromString("TestContractA"); // 0x46c86551bca0e3120ca0f831f53d8cb55ac7
            var addrB = Address.FromString("TestContractB"); // 0xea0e38633e550dc4b7914010c2d7c95086ee
            var addrC = Address.FromString("TestContractC"); // 0x053f751c35f7c681be14bcee03085dc8a309
            
            await _functionMetadataService.DeployContract(chainId, addrC, contractCTemplate);
            await _functionMetadataService.DeployContract(chainId, addrB, contractBTemplate);
            await _functionMetadataService.DeployContract(chainId, addrA, contractATemplate);
            
            await _functionMetadataService.DeployContract(chainId, contract1Addr, nonAttrContract1Template);
            await _functionMetadataService.DeployContract(chainId, contract2Addr, nonAttrContract2Template);
            await _functionMetadataService.DeployContract(chainId, refContractAddr, refNonAttrContractTemplate);
      
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrC.DumpHex() + ".Func0"))));

            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing) 
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrC.DumpHex() + ".Func1"))));
                        
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrC.DumpHex() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrB.DumpHex() + ".Func0"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrB.DumpHex() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>()), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.DumpHex() + ".Func0(int)"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.DumpHex() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.DumpHex() + ".Func0"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.DumpHex() + ".Func2"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.DumpHex() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.DumpHex() + ".Func2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.DumpHex() + ".Func0",
                    addrB.DumpHex() + ".Func0", 
                    addrC.DumpHex() + ".Func0"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.DumpHex() + ".Func3"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.DumpHex() + ".Func2"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.DumpHex() + ".Func4"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.DumpHex() + ".Func3",
                    addrB.DumpHex() + ".Func1"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.DumpHex() + ".Func5"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, contract1Addr.DumpHex() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, contract1Addr.DumpHex() + ".Func2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(contract2Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, contract2Addr.DumpHex() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(contract2Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, contract2Addr.DumpHex() + ".Func2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []{contract1Addr.DumpHex() + ".Func1"}),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.DumpHex() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []{contract1Addr.DumpHex() + ".Func1"}),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    new Resource(refContractAddr.Value.ToByteArray().ToHex() + ".localRes", DataAccessMode.AccountSpecific)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.DumpHex() + ".Func1_1"))));
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []{contract1Addr.DumpHex() + ".Func2"}),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.DumpHex() + ".Func2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    contract1Addr.DumpHex() + ".Func2",
                    refContractAddr.DumpHex() + ".Func1_1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    new Resource(refContractAddr.Value.ToByteArray().ToHex() + ".localRes", DataAccessMode.AccountSpecific)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.DumpHex() + ".Func2_1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    contract1Addr.DumpHex() + ".Func2",
                    refContractAddr.DumpHex() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.DumpHex() + ".Func2_2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    contract1Addr.DumpHex() + ".Func1",
                    contract1Addr.DumpHex() + ".Func2"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.DumpHex() + ".Func3"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    contract1Addr.DumpHex() + ".Func1",
                    contract2Addr.DumpHex() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    new Resource(contract2Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.DumpHex() + ".Func4"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    contract1Addr.DumpHex() + ".Func2",
                    addrC.DumpHex() + ".Func0"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific),
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.DumpHex() + ".Func5"))));
            
            var callGraph = new SerializedCallGraph
            {
                Vertices =
                {
                    addrC.DumpHex() + ".Func0",
                    addrC.DumpHex() + ".Func1",
                    addrB.DumpHex() + ".Func0",
                    addrB.DumpHex() + ".Func1",
                    addrA.DumpHex() + ".Func0(int)",
                    addrA.DumpHex() + ".Func0",
                    addrA.DumpHex() + ".Func1",
                    addrA.DumpHex() + ".Func2",
                    addrA.DumpHex() + ".Func3",
                    addrA.DumpHex() + ".Func4",
                    addrA.DumpHex() + ".Func5",
                    
                    contract1Addr.DumpHex() + ".Func1",
                    contract1Addr.DumpHex() + ".Func2",
                    contract2Addr.DumpHex() + ".Func1",
                    contract2Addr.DumpHex() + ".Func2",
                    refContractAddr.DumpHex() + ".Func1",
                    refContractAddr.DumpHex() + ".Func1_1",
                    refContractAddr.DumpHex() + ".Func2",
                    refContractAddr.DumpHex() + ".Func2_1",
                    refContractAddr.DumpHex() + ".Func2_2",
                    refContractAddr.DumpHex() + ".Func3",
                    refContractAddr.DumpHex() + ".Func4",
                    refContractAddr.DumpHex() + ".Func5"
                },
                Edges =
                {
                    new GraphEdge
                    {
                        Source = addrB.DumpHex() + ".Func0",
                        Target = addrC.DumpHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func0",
                        Target = addrA.DumpHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func1",
                        Target = addrA.DumpHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func3",
                        Target = addrB.DumpHex() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func3",
                        Target = addrA.DumpHex() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func3",
                        Target = addrC.DumpHex() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func4",
                        Target = addrA.DumpHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func5",
                        Target = addrB.DumpHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.DumpHex() + ".Func5",
                        Target = addrA.DumpHex() + ".Func3"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.DumpHex() + ".Func1",
                        Target = contract1Addr.DumpHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.DumpHex() + ".Func1_1",
                        Target = contract1Addr.DumpHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.DumpHex() + ".Func2",
                        Target = contract1Addr.DumpHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.DumpHex() + ".Func2_1",
                        Target = contract1Addr.DumpHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.DumpHex() + ".Func2_1",
                        Target = refContractAddr.DumpHex() + ".Func1_1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.DumpHex() + ".Func2_2",
                        Target = contract1Addr.DumpHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.DumpHex() + ".Func2_2",
                        Target = refContractAddr.DumpHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.DumpHex() + ".Func3",
                        Target = contract1Addr.DumpHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.DumpHex() + ".Func3",
                        Target = contract1Addr.DumpHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.DumpHex() + ".Func4",
                        Target = contract1Addr.DumpHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.DumpHex() + ".Func4",
                        Target = contract2Addr.DumpHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.DumpHex() + ".Func5",
                        Target = contract1Addr.DumpHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.DumpHex() + ".Func5",
                        Target = addrC.DumpHex() + ".Func0"
                    }
                }
            };
            Assert.Equal(callGraph, await _dataStore.GetAsync<SerializedCallGraph>(chainId.OfType(HashType.CallingGraph)));
        }
    
    }
        
    public class TestContractC
    {
        [SmartContractFieldData("${this}.resource4", DataAccessMode.AccountSpecific)]
        public int resource4;

        [SmartContractFieldData("${this}.resource5", DataAccessMode.ReadOnlyAccountSharing)]
        private int resource5;

        [SmartContractFunction("${this}.Func0", new string[] { }, new[] {"${this}.resource4"})]
        public void Func0()
        {
        }

        [SmartContractFunction("${this}.Func1", new string[] { }, new[] {"${this}.resource5"})]
        public void Func1()
        {
        }
    }

    internal class TestContractB
    {
        [SmartContractFieldData("${this}.resource2", DataAccessMode.AccountSpecific)]
        public int resource2;

        [SmartContractFieldData("${this}.resource3", DataAccessMode.ReadOnlyAccountSharing)]
        private int resource3;

        [SmartContractReference("ContractC", "0x053f751c35f7c681be14bcee03085dc8a309")]
        public TestContractC ContractC;

        [SmartContractFunction("${this}.Func0", new[] {"${ContractC}.Func1"}, new[] {"${this}.resource2"})]
        public void Func0()
        {
        }

        [SmartContractFunction("${this}.Func1", new string[] { }, new[] {"${this}.resource3"})]
        public void Func1()
        {
        }
    }

    internal class TestContractA
    {
        //test for different accessibility
        [SmartContractFieldData("${this}.resource0", DataAccessMode.AccountSpecific)]
        public int resource0;

        [SmartContractFieldData("${this}.resource1", DataAccessMode.ReadOnlyAccountSharing)]
        private int resource1;

        [SmartContractFieldData("${this}.resource2", DataAccessMode.ReadWriteAccountSharing)]
        protected int resource2;

        [SmartContractReference("_contractB", "0xea0e38633e550dc4b7914010c2d7c95086ee")]
        private TestContractB _contractB;

        [SmartContractReference("ContractC", "0x053f751c35f7c681be14bcee03085dc8a309")]
        public TestContractC ContractC;


        //test for empty calling set and resource set
        [SmartContractFunction("${this}.Func0(int)", new string[] { }, new string[] { })]
        private void Func0(int a)
        {
        }


        //test for same func name but different parameter
        //test for local function references recursive (resource completation)
        [SmartContractFunction("${this}.Func0", new[] {"${this}.Func1"}, new string[] {"${this}.resource0"})]
        public void Func0()
        {
        }

        //test for local function reference non-recursive and test for overlap resource set
        [SmartContractFunction("${this}.Func1", new[] {"${this}.Func2"}, new[] {"${this}.resource1"})]
        public void Func1()
        {
        }

        //test for foreign contract, test for duplicate local resource
        //when deploy: test for recursive foreign resource collect
        [SmartContractFunction("${this}.Func2", new string[] { }, new[] {"${this}.resource1", "${this}.resource2"})]
        protected void Func2()
        {
        }

        //test for foreign calling set only
        [SmartContractFunction("${this}.Func3", new[] {"${_contractB}.Func0", "${this}.Func0", "${ContractC}.Func0"},
            new[] {"${this}.resource1"})]
        public void Func3()
        {
        }

        //test for duplication in calling set
        [SmartContractFunction("${this}.Func4", new[] {"${this}.Func2", "${this}.Func2"}, new string[] { })]
        public void Func4()
        {
        }

        //test for duplicate foreign call
        [SmartContractFunction("${this}.Func5", new[] {"${_contractB}.Func1", "${this}.Func3"}, new string[] { })]
        public void Func5()
        {
        }
    }

    public class TestRefNonAttrContract : CSharpSmartContract
    {
        [SmartContractReference("ref1", "0x3f77405cbfe1e48e2fa0e4bf6a4e5917f768")]
        public TestNonAttrContract1 ref1;
        
        [SmartContractReference("ref2", "0xb4e0cc36ede5d518fbabd1ed5498093e4b71")]
        public TestNonAttrContract2 ref2;

        [SmartContractReference("refc", "0x053f751c35f7c681be14bcee03085dc8a309")]
        public TestContractC refc;

        [SmartContractFieldData("${this}.localRes", DataAccessMode.AccountSpecific)]
        public int localRes;

        [SmartContractFunction("${this}.Func1", new []{"${ref1}.Func1"}, new string[]{})]
        public void Func1()
        {
            
        }
        
        [SmartContractFunction("${this}.Func1_1", new []{"${ref1}.Func1"}, new string[]{"${this}.localRes"})]
        public void Func1_1()
        {
            
        }
        
        [SmartContractFunction("${this}.Func2", new []{"${ref1}.Func2"}, new string[]{})]
        public void Func2()
        {
            
        }

        [SmartContractFunction("${this}.Func2_1", new []{"${ref1}.Func2", "${this}.Func1_1"}, new string[]{})]
        public void Func2_1()
        {
            
        }
        
        [SmartContractFunction("${this}.Func2_2", new []{"${ref1}.Func2", "${this}.Func1"}, new string[]{})]
        public void Func2_2()
        {
            
        }
        
        [SmartContractFunction("${this}.Func3", new []{"${ref1}.Func1", "${ref1}.Func2"}, new string[]{})]
        public void Func3()
        {
            
        }
        
        [SmartContractFunction("${this}.Func4", new []{"${ref1}.Func1", "${ref2}.Func1"}, new string[]{})]
        public void Func4()
        {
            
        }
        
        [SmartContractFunction("${this}.Func5", new []{"${ref1}.Func2", "${refc}.Func0"}, new string[]{})]
        public void Func5()
        {
            
        }

        
    }
    
    public class TestNonAttrContract1 : CSharpSmartContract
    {
        public void Func1()
        {
            
        }
        
        public void Func2()
        {
            
        }
    }
    
    public class TestNonAttrContract2 : CSharpSmartContract
    {
        public void Func1()
        {
            
        }
        
        public void Func2()
        {
            
        }
    }
}