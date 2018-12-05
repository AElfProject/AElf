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
            var chainId = Hash.LoadByteArray(new byte[] {0x01, 0x02, 0x03});
            var runner = _smartContractRunnerFactory.GetRunner(0);
            var contractCType = typeof(TestContractC);
            var contractBType = typeof(TestContractB);
            var contractAType = typeof(TestContractA);
            
            var contractCTemplate = runner.ExtractMetadata(contractCType);
            var contractBTemplate = runner.ExtractMetadata(contractBType);
            var contractATemplate = runner.ExtractMetadata(contractAType);
            
            var addrA = Address.FromString("ELF_1234_TestContractA");
            var addrB = Address.FromString("ELF_1234_TestContractB");
            var addrC = Address.FromString("ELF_1234_TestContractC");

            await _functionMetadataService.DeployContract(chainId, addrC, contractCTemplate);

            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.GetFormatted() + ".resource4", DataAccessMode.AccountSpecific)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrC.GetFormatted() + ".Func0")));

            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.GetFormatted() + ".resource5", DataAccessMode.ReadOnlyAccountSharing) 
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrC.GetFormatted() + ".Func1")));
            
            await _functionMetadataService.DeployContract(chainId, addrB, contractBTemplate);
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrC.GetFormatted() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(addrC.GetFormatted() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrB.GetFormatted() + ".resource2", DataAccessMode.AccountSpecific), 
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrB.GetFormatted() + ".Func0")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new []
                {
                    new Resource(addrB.GetFormatted() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrB.GetFormatted() + ".Func1")));

            await _functionMetadataService.DeployContract(chainId, addrA, contractATemplate);
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>()), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.GetFormatted() + ".Func0(int)")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.GetFormatted() + ".Func1"
                }),
                new HashSet<Resource>(new []
                {
                    new Resource(addrA.GetFormatted() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrA.GetFormatted() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.GetFormatted() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.GetFormatted() + ".Func0")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.GetFormatted() + ".Func2"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.GetFormatted() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.GetFormatted() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.GetFormatted() + ".Func1")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.GetFormatted() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.GetFormatted() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.GetFormatted() + ".Func2")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.GetFormatted() + ".Func0",
                    addrB.GetFormatted() + ".Func0", 
                    addrC.GetFormatted() + ".Func0"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.GetFormatted() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrB.GetFormatted() + ".resource2", DataAccessMode.AccountSpecific), 
                    new Resource(addrC.GetFormatted() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrC.GetFormatted() + ".resource4", DataAccessMode.AccountSpecific),
                    new Resource(addrA.GetFormatted() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.GetFormatted() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.GetFormatted() + ".Func3")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.GetFormatted() + ".Func2"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.GetFormatted() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.GetFormatted() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.GetFormatted() + ".Func4")));
            
            Assert.Equal(new FunctionMetadata(
                new HashSet<string>(new []
                {
                    addrA.GetFormatted() + ".Func3",
                    addrB.GetFormatted() + ".Func1"
                }),
                new HashSet<Resource>(new[]
                {
                    new Resource(addrA.GetFormatted() + ".resource0", DataAccessMode.AccountSpecific),
                    new Resource(addrB.GetFormatted() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                    new Resource(addrB.GetFormatted() + ".resource2", DataAccessMode.AccountSpecific), 
                    new Resource(addrC.GetFormatted() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrC.GetFormatted() + ".resource4", DataAccessMode.AccountSpecific),
                    new Resource(addrA.GetFormatted() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource(addrA.GetFormatted() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                })), await _dataStore.GetAsync<FunctionMetadata>(DataPath.CalculatePointerForMetadata(chainId, addrA.GetFormatted() + ".Func5")));

            var callGraph = new SerializedCallGraph
            {
                Vertices =
                {
                    addrC.GetFormatted() + ".Func0",
                    addrC.GetFormatted() + ".Func1",
                    addrB.GetFormatted() + ".Func0",
                    addrB.GetFormatted() + ".Func1",
                    addrA.GetFormatted() + ".Func0(int)",
                    addrA.GetFormatted() + ".Func0",
                    addrA.GetFormatted() + ".Func1",
                    addrA.GetFormatted() + ".Func2",
                    addrA.GetFormatted() + ".Func3",
                    addrA.GetFormatted() + ".Func4",
                    addrA.GetFormatted() + ".Func5"
                },
                Edges =
                {
                    new GraphEdge
                    {
                        Source = addrB.GetFormatted() + ".Func0",
                        Target = addrC.GetFormatted() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.GetFormatted() + ".Func0",
                        Target = addrA.GetFormatted() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.GetFormatted() + ".Func1",
                        Target = addrA.GetFormatted() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = addrA.GetFormatted() + ".Func3",
                        Target = addrB.GetFormatted() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.GetFormatted() + ".Func3",
                        Target = addrA.GetFormatted() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.GetFormatted() + ".Func3",
                        Target = addrC.GetFormatted() + ".Func0"
                    },
                    new GraphEdge
                    {
                        Source = addrA.GetFormatted() + ".Func4",
                        Target = addrA.GetFormatted() + ".Func2"
                    },
                    new GraphEdge
                    {
                        Source = addrA.GetFormatted() + ".Func5",
                        Target = addrB.GetFormatted() + ".Func1"
                    },
                    new GraphEdge
                    {
                        Source = addrA.GetFormatted() + ".Func5",
                        Target = addrA.GetFormatted() + ".Func3"
                    }
                }
            };
            Assert.Equal(callGraph, await _dataStore.GetAsync<SerializedCallGraph>(chainId.OfType(HashType.CallingGraph)));
        }
    }
}