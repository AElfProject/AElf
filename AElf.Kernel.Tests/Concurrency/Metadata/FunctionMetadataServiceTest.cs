using System.Collections.Generic;
using System.Threading.Tasks;
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
            
            var addrA = new Hash("TestContractA".CalculateHash()).ToAccount();
            var addrB = new Hash("TestContractB".CalculateHash()).ToAccount();
            var addrC = new Hash("TestContractC".CalculateHash()).ToAccount();

            await _functionMetadataService.DeployContract(chainId, addrC, contractCTemplate);

            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrC.ToHex() + ".Func0"))));

            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing) 
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrC.ToHex() + ".Func1"))));
            
            await _functionMetadataService.DeployContract(chainId, addrB, contractBTemplate);
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrC.ToHex() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrB.ToHex() + ".Func0"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrB.ToHex() + ".Func1"))));

            await _functionMetadataService.DeployContract(chainId, addrA, contractATemplate);
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>()), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrA.ToHex() + ".Func0(int)"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.ToHex() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrA.ToHex() + ".Func0"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.ToHex() + ".Func2"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrA.ToHex() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrA.ToHex() + ".Func2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.ToHex() + ".Func0",
                    addrB.ToHex() + ".Func0", 
                    addrC.ToHex() + ".Func0"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrA.ToHex() + ".Func3"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.ToHex() + ".Func2"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrA.ToHex() + ".Func4"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.ToHex() + ".Func3",
                    addrB.ToHex() + ".Func1"
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
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrA.ToHex() + ".Func5"))));

            var callGraph = new SerializedCallGraph
            {
                Vertices =
                {
                    addrC.ToHex() + ".Func0",
                    addrC.ToHex() + ".Func1",
                    addrB.ToHex() + ".Func0",
                    addrB.ToHex() + ".Func1",
                    addrA.ToHex() + ".Func0(int)",
                    addrA.ToHex() + ".Func0",
                    addrA.ToHex() + ".Func1",
                    addrA.ToHex() + ".Func2",
                    addrA.ToHex() + ".Func3",
                    addrA.ToHex() + ".Func4",
                    addrA.ToHex() + ".Func5"
                },
                Edges =
                {
                    new GraphEdge
                    {
                        Source = addrB.ToHex() + ".Func0",
                        Target = addrC.ToHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func0",
                        Target = addrA.ToHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func1",
                        Target = addrA.ToHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func3",
                        Target = addrB.ToHex() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func3",
                        Target = addrA.ToHex() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func3",
                        Target = addrC.ToHex() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func4",
                        Target = addrA.ToHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func5",
                        Target = addrB.ToHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func5",
                        Target = addrA.ToHex() + ".Func3"
                    }
                }
            };
            Assert.Equal(callGraph, SerializedCallGraph.Parser.ParseFrom(await _dataStore.GetDataAsync<SerializedCallGraph>(ResourcePath.CalculatePointerForMetadataTemplateCallingGraph(chainId))));
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
            
            
            var contract1Addr = new Hash("TestNonAttrContract1".CalculateHash()).ToAccount();
            var contract2Addr = new Hash("TestNonAttrContract2".CalculateHash()).ToAccount();
            var refContractAddr = new Hash("TestRefNonAttrContract".CalculateHash()).ToAccount();
            
            var addrA = new Hash("TestContractA".CalculateHash()).ToAccount();
            var addrB = new Hash("TestContractB".CalculateHash()).ToAccount();
            var addrC = new Hash("TestContractC".CalculateHash()).ToAccount();
            
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
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrC.ToHex() + ".Func0"))));

            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing) 
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrC.ToHex() + ".Func1"))));
                        
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrC.ToHex() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrB.ToHex() + ".Func0"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrB.ToHex() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>()), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrA.ToHex() + ".Func0(int)"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.ToHex() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrA.ToHex() + ".Func0"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.ToHex() + ".Func2"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrA.ToHex() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrA.ToHex() + ".Func2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.ToHex() + ".Func0",
                    addrB.ToHex() + ".Func0", 
                    addrC.ToHex() + ".Func0"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrA.ToHex() + ".Func3"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.ToHex() + ".Func2"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrA.ToHex() + ".Func4"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.ToHex() + ".Func3",
                    addrB.ToHex() + ".Func1"
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
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, addrA.ToHex() + ".Func5"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, contract1Addr.ToHex() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, contract1Addr.ToHex() + ".Func2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(contract2Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, contract2Addr.ToHex() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(contract2Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, contract2Addr.ToHex() + ".Func2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []{contract1Addr.ToHex() + ".Func1"}),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, refContractAddr.ToHex() + ".Func1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []{contract1Addr.ToHex() + ".Func1"}),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    new Resource(refContractAddr.Value.ToByteArray().ToHex() + ".localRes", DataAccessMode.AccountSpecific)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, refContractAddr.ToHex() + ".Func1_1"))));
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []{contract1Addr.ToHex() + ".Func2"}),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, refContractAddr.ToHex() + ".Func2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    contract1Addr.ToHex() + ".Func2",
                    refContractAddr.ToHex() + ".Func1_1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    new Resource(refContractAddr.Value.ToByteArray().ToHex() + ".localRes", DataAccessMode.AccountSpecific)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, refContractAddr.ToHex() + ".Func2_1"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    contract1Addr.ToHex() + ".Func2",
                    refContractAddr.ToHex() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, refContractAddr.ToHex() + ".Func2_2"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    contract1Addr.ToHex() + ".Func1",
                    contract1Addr.ToHex() + ".Func2"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, refContractAddr.ToHex() + ".Func3"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    contract1Addr.ToHex() + ".Func1",
                    contract2Addr.ToHex() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    new Resource(contract2Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, refContractAddr.ToHex() + ".Func4"))));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    contract1Addr.ToHex() + ".Func2",
                    addrC.ToHex() + ".Func0"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific),
                })), FunctionMetadata.Parser.ParseFrom(await _dataStore.GetDataAsync<FunctionMetadata>(ResourcePath.CalculatePointerForMetadata(chainId, refContractAddr.ToHex() + ".Func5"))));
            
            var callGraph = new SerializedCallGraph
            {
                Vertices =
                {
                    addrC.ToHex() + ".Func0",
                    addrC.ToHex() + ".Func1",
                    addrB.ToHex() + ".Func0",
                    addrB.ToHex() + ".Func1",
                    addrA.ToHex() + ".Func0(int)",
                    addrA.ToHex() + ".Func0",
                    addrA.ToHex() + ".Func1",
                    addrA.ToHex() + ".Func2",
                    addrA.ToHex() + ".Func3",
                    addrA.ToHex() + ".Func4",
                    addrA.ToHex() + ".Func5",
                    
                    contract1Addr.ToHex() + ".Func1",
                    contract1Addr.ToHex() + ".Func2",
                    contract2Addr.ToHex() + ".Func1",
                    contract2Addr.ToHex() + ".Func2",
                    refContractAddr.ToHex() + ".Func1",
                    refContractAddr.ToHex() + ".Func1_1",
                    refContractAddr.ToHex() + ".Func2",
                    refContractAddr.ToHex() + ".Func2_1",
                    refContractAddr.ToHex() + ".Func2_2",
                    refContractAddr.ToHex() + ".Func3",
                    refContractAddr.ToHex() + ".Func4",
                    refContractAddr.ToHex() + ".Func5"
                },
                Edges =
                {
                    new GraphEdge
                    {
                        Source = addrB.ToHex() + ".Func0",
                        Target = addrC.ToHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func0",
                        Target = addrA.ToHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func1",
                        Target = addrA.ToHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func3",
                        Target = addrB.ToHex() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func3",
                        Target = addrA.ToHex() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func3",
                        Target = addrC.ToHex() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func4",
                        Target = addrA.ToHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func5",
                        Target = addrB.ToHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.ToHex() + ".Func5",
                        Target = addrA.ToHex() + ".Func3"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.ToHex() + ".Func1",
                        Target = contract1Addr.ToHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.ToHex() + ".Func1_1",
                        Target = contract1Addr.ToHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.ToHex() + ".Func2",
                        Target = contract1Addr.ToHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.ToHex() + ".Func2_1",
                        Target = contract1Addr.ToHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.ToHex() + ".Func2_1",
                        Target = refContractAddr.ToHex() + ".Func1_1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.ToHex() + ".Func2_2",
                        Target = contract1Addr.ToHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.ToHex() + ".Func2_2",
                        Target = refContractAddr.ToHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.ToHex() + ".Func3",
                        Target = contract1Addr.ToHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.ToHex() + ".Func3",
                        Target = contract1Addr.ToHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.ToHex() + ".Func4",
                        Target = contract1Addr.ToHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.ToHex() + ".Func4",
                        Target = contract2Addr.ToHex() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.ToHex() + ".Func5",
                        Target = contract1Addr.ToHex() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = refContractAddr.ToHex() + ".Func5",
                        Target = addrC.ToHex() + ".Func0"
                    }
                }
            };
            Assert.Equal(callGraph, SerializedCallGraph.Parser.ParseFrom(await _dataStore.GetDataAsync<SerializedCallGraph>(ResourcePath.CalculatePointerForMetadataTemplateCallingGraph(chainId))));
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