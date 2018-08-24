using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common.Extensions;
using AElf.SmartContract;
using AElf.Kernel.Storages;
using AElf.Kernel.Tests.Concurrency.Scheduling;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using ServiceStack;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.Concurrency.Metadata
{
    [UseAutofacTestFramework]
    public class ChainFunctionMetadataTest
    {        
        private readonly IDataStore _dataStore;
        private readonly ISmartContractRunnerFactory _smartContractRunnerFactory;
        private readonly IFunctionMetadataService _functionMetadataService;

        public ChainFunctionMetadataTest(IDataStore templateStore, ISmartContractRunnerFactory smartContractRunnerFactory, IFunctionMetadataService functionMetadataService)
        {
            _dataStore = templateStore ?? throw new ArgumentNullException(nameof(templateStore));
            _smartContractRunnerFactory = smartContractRunnerFactory ?? throw new ArgumentException(nameof(smartContractRunnerFactory));
            _functionMetadataService = functionMetadataService ?? throw new ArgumentException(nameof(functionMetadataService));
        }
        
        [Fact]
        public async Task TestDeployNewFunction()
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
    }
}