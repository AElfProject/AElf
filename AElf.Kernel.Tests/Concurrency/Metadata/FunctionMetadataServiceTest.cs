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

            await _functionMetadataService.DeployContract(chainId, addrC, contractCTemplate);

            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrC.Dumps() + ".Func0")));

            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing) 
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrC.Dumps() + ".Func1")));
            
            await _functionMetadataService.DeployContract(chainId, addrB, contractBTemplate);
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrC.Dumps() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrB.Dumps() + ".Func0")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrB.Dumps() + ".Func1")));

            await _functionMetadataService.DeployContract(chainId, addrA, contractATemplate);
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>()), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.Dumps() + ".Func0(int)")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.Dumps() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.Dumps() + ".Func0")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.Dumps() + ".Func2"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.Dumps() + ".Func1")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.Dumps() + ".Func2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.Dumps() + ".Func0",
                    addrB.Dumps() + ".Func0", 
                    addrC.Dumps() + ".Func0"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.Dumps() + ".Func3")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.Dumps() + ".Func2"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.Dumps() + ".Func4")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.Dumps() + ".Func3",
                    addrB.Dumps() + ".Func1"
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
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.Dumps() + ".Func5")));

            var callGraph = new SerializedCallGraph
            {
                Vertices =
                {
                    addrC.Dumps() + ".Func0",
                    addrC.Dumps() + ".Func1",
                    addrB.Dumps() + ".Func0",
                    addrB.Dumps() + ".Func1",
                    addrA.Dumps() + ".Func0(int)",
                    addrA.Dumps() + ".Func0",
                    addrA.Dumps() + ".Func1",
                    addrA.Dumps() + ".Func2",
                    addrA.Dumps() + ".Func3",
                    addrA.Dumps() + ".Func4",
                    addrA.Dumps() + ".Func5"
                },
                Edges =
                {
                    new GraphEdge
                    {
                        Source = addrB.Dumps() + ".Func0",
                        Target = addrC.Dumps() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func0",
                        Target = addrA.Dumps() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func1",
                        Target = addrA.Dumps() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func3",
                        Target = addrB.Dumps() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func3",
                        Target = addrA.Dumps() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func3",
                        Target = addrC.Dumps() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func4",
                        Target = addrA.Dumps() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func5",
                        Target = addrB.Dumps() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func5",
                        Target = addrA.Dumps() + ".Func3"
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
            
            
            var contract1Addr = Address.FromString("TestNonAttrContract1");
            var contract2Addr = Address.FromString("TestNonAttrContract2");
            var refContractAddr = Address.FromString("TestRefNonAttrContract");
            
            var addrA = Address.FromString("TestContractA");
            var addrB = Address.FromString("TestContractB");
            var addrC = Address.FromString("TestContractC");
            
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
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrC.Dumps() + ".Func0"))));

            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing) 
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrC.Dumps() + ".Func1"))));
                        
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrC.Dumps() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrB.Dumps() + ".Func0"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrB.Dumps() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>()), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.Dumps() + ".Func0(int)"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.Dumps() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.Dumps() + ".Func0"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.Dumps() + ".Func2"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.Dumps() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.Dumps() + ".Func2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.Dumps() + ".Func0",
                    addrB.Dumps() + ".Func0", 
                    addrC.Dumps() + ".Func0"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.Dumps() + ".Func3"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.Dumps() + ".Func2"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.Dumps() + ".Func4"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.Dumps() + ".Func3",
                    addrB.Dumps() + ".Func1"
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
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.Dumps() + ".Func5"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, contract1Addr.Dumps() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, contract1Addr.Dumps() + ".Func2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(contract2Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, contract2Addr.Dumps() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(contract2Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, contract2Addr.Dumps() + ".Func2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []{contract1Addr.Dumps() + ".Func1"}),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.Dumps() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []{contract1Addr.Dumps() + ".Func1"}),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    new Resource(refContractAddr.Value.ToByteArray().ToHex() + ".localRes", DataAccessMode.AccountSpecific)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.Dumps() + ".Func1_1"))));
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []{contract1Addr.Dumps() + ".Func2"}),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.Dumps() + ".Func2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    contract1Addr.Dumps() + ".Func2",
                    refContractAddr.Dumps() + ".Func1_1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    new Resource(refContractAddr.Value.ToByteArray().ToHex() + ".localRes", DataAccessMode.AccountSpecific)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.Dumps() + ".Func2_1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    contract1Addr.Dumps() + ".Func2",
                    refContractAddr.Dumps() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.Dumps() + ".Func2_2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    contract1Addr.Dumps() + ".Func1",
                    contract1Addr.Dumps() + ".Func2"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.Dumps() + ".Func3"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    contract1Addr.Dumps() + ".Func1",
                    contract2Addr.Dumps() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    new Resource(contract2Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.Dumps() + ".Func4"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    contract1Addr.Dumps() + ".Func2",
                    addrC.Dumps() + ".Func0"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific),
                })), (await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, refContractAddr.Dumps() + ".Func5"))));
            
            var callGraph = new SerializedCallGraph
            {
                Vertices =
                {
                    addrC.Dumps() + ".Func0",
                    addrC.Dumps() + ".Func1",
                    addrB.Dumps() + ".Func0",
                    addrB.Dumps() + ".Func1",
                    addrA.Dumps() + ".Func0(int)",
                    addrA.Dumps() + ".Func0",
                    addrA.Dumps() + ".Func1",
                    addrA.Dumps() + ".Func2",
                    addrA.Dumps() + ".Func3",
                    addrA.Dumps() + ".Func4",
                    addrA.Dumps() + ".Func5",
                    
                    contract1Addr.Dumps() + ".Func1",
                    contract1Addr.Dumps() + ".Func2",
                    contract2Addr.Dumps() + ".Func1",
                    contract2Addr.Dumps() + ".Func2",
                    refContractAddr.Dumps() + ".Func1",
                    refContractAddr.Dumps() + ".Func1_1",
                    refContractAddr.Dumps() + ".Func2",
                    refContractAddr.Dumps() + ".Func2_1",
                    refContractAddr.Dumps() + ".Func2_2",
                    refContractAddr.Dumps() + ".Func3",
                    refContractAddr.Dumps() + ".Func4",
                    refContractAddr.Dumps() + ".Func5"
                },
                Edges =
                {
                    new GraphEdge
                    {
                        Source = addrB.Dumps() + ".Func0",
                        Target = addrC.Dumps() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func0",
                        Target = addrA.Dumps() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func1",
                        Target = addrA.Dumps() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func3",
                        Target = addrB.Dumps() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func3",
                        Target = addrA.Dumps() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func3",
                        Target = addrC.Dumps() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func4",
                        Target = addrA.Dumps() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func5",
                        Target = addrB.Dumps() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.Dumps() + ".Func5",
                        Target = addrA.Dumps() + ".Func3"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.Dumps() + ".Func1",
                        Target = contract1Addr.Dumps() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.Dumps() + ".Func1_1",
                        Target = contract1Addr.Dumps() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.Dumps() + ".Func2",
                        Target = contract1Addr.Dumps() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.Dumps() + ".Func2_1",
                        Target = contract1Addr.Dumps() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.Dumps() + ".Func2_1",
                        Target = refContractAddr.Dumps() + ".Func1_1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.Dumps() + ".Func2_2",
                        Target = contract1Addr.Dumps() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.Dumps() + ".Func2_2",
                        Target = refContractAddr.Dumps() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.Dumps() + ".Func3",
                        Target = contract1Addr.Dumps() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.Dumps() + ".Func3",
                        Target = contract1Addr.Dumps() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.Dumps() + ".Func4",
                        Target = contract1Addr.Dumps() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.Dumps() + ".Func4",
                        Target = contract2Addr.Dumps() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.Dumps() + ".Func5",
                        Target = contract1Addr.Dumps() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.Dumps() + ".Func5",
                        Target = addrC.Dumps() + ".Func0"
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

        [SmartContractReference("ContractC", "0x38f3a6b010cadfa690cc1900241c053f751c")]
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

        [SmartContractReference("_contractB", "0x22e7340eb68c9a01804aabeb10c2ea0e3863")]
        private TestContractB _contractB;

        [SmartContractReference("ContractC", "0x38f3a6b010cadfa690cc1900241c053f751c")]
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
        [SmartContractReference("ref1", "0x2829f1f4f08735a2325dfb7e41023f77405c")]
        public TestNonAttrContract1 ref1;
        
        [SmartContractReference("ref2", "0x205bd293801714a67d5eba1e00abb4e0cc36")]
        public TestNonAttrContract2 ref2;

        [SmartContractReference("refc", "0x38f3a6b010cadfa690cc1900241c053f751c")]
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