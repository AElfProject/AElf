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
using AElf.Common;

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
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.Dumps() + ".Func2")));
            
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
    }
}