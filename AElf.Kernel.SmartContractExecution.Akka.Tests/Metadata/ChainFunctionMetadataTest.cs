//using System.Collections.Generic;
//using System.Threading.Tasks;
//using AElf.SmartContract;
//using AElf.Kernel.Core.Tests.Concurrency.Scheduling;
//using Google.Protobuf;
//using Microsoft.Extensions.Logging;
//using Xunit;
//using AElf.Common;
//using AElf.TestBase;
//using AElf.Kernel.Managers;
//using AElf.Kernel.SmartContract;
//using AElf.Kernel.SmartContract.Domain;
//using FunctionMetadata = AElf.Kernel.SmartContract.FunctionMetadata;

namespace AElf.Kernel.Tests.Concurrency.Metadata
{
    /*
    public sealed class ChainFunctionMetadataTest : AElfKernelTestBase
    {
        private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
        private readonly IFunctionMetadataService _functionMetadataService;
        private readonly IFunctionMetadataManager _functionMetadataManager;

        public ChainFunctionMetadataTest()
        {
            _smartContractRunnerContainer = GetRequiredService<ISmartContractRunnerContainer>();
            _functionMetadataService = GetRequiredService<IFunctionMetadataService>();
            _functionMetadataManager = GetRequiredService<IFunctionMetadataManager>();
        }

        [Fact]
        public async Task TestDeployNewFunction()
        {
            var chainId = ChainHelpers.GetChainId(101);
            var runner = _smartContractRunnerContainer.GetRunner(0);
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
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrC.GetFormatted() + ".resource4", DataAccessMode.AccountSpecific)
                    })),
                await _functionMetadataManager.GetMetadataAsync(chainId, addrC.GetFormatted() + ".Func0"));

            Assert.Equal(new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrC.GetFormatted() + ".resource5", DataAccessMode.ReadOnlyAccountSharing)
                    })),
                await _functionMetadataManager.GetMetadataAsync(chainId, addrC.GetFormatted() + ".Func1"));

            await _functionMetadataService.DeployContract(chainId, addrB, contractBTemplate);

            Assert.Equal(new FunctionMetadata(
                    new HashSet<string>(new[]
                    {
                        addrC.GetFormatted() + ".Func1"
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrC.GetFormatted() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrB.GetFormatted() + ".resource2", DataAccessMode.AccountSpecific),
                    })),
                await _functionMetadataManager.GetMetadataAsync(chainId, addrB.GetFormatted() + ".Func0"));

            Assert.Equal(new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrB.GetFormatted() + ".resource3", DataAccessMode.ReadOnlyAccountSharing),
                    })),
                await _functionMetadataManager.GetMetadataAsync(chainId, addrB.GetFormatted() + ".Func1"));

            await _functionMetadataService.DeployContract(chainId, addrA, contractATemplate);

            Assert.Equal(new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>()),
                await _functionMetadataManager.GetMetadataAsync(chainId, addrA.GetFormatted() + ".Func0(int)"));

            Assert.Equal(new FunctionMetadata(
                    new HashSet<string>(new[]
                    {
                        addrA.GetFormatted() + ".Func1"
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.GetFormatted() + ".resource0", DataAccessMode.AccountSpecific),
                        new Resource(addrA.GetFormatted() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.GetFormatted() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    })),
                await _functionMetadataManager.GetMetadataAsync(chainId, addrA.GetFormatted() + ".Func0"));

            Assert.Equal(new FunctionMetadata(
                    new HashSet<string>(new[]
                    {
                        addrA.GetFormatted() + ".Func2"
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.GetFormatted() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.GetFormatted() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    })),
                await _functionMetadataManager.GetMetadataAsync(chainId, addrA.GetFormatted() + ".Func1"));

            Assert.Equal(new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.GetFormatted() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.GetFormatted() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    })),
                await _functionMetadataManager.GetMetadataAsync(chainId, addrA.GetFormatted() + ".Func2"));

            Assert.Equal(new FunctionMetadata(
                    new HashSet<string>(new[]
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
                    })),
                await _functionMetadataManager.GetMetadataAsync(chainId, addrA.GetFormatted() + ".Func3"));

            Assert.Equal(new FunctionMetadata(
                    new HashSet<string>(new[]
                    {
                        addrA.GetFormatted() + ".Func2"
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.GetFormatted() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.GetFormatted() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    })),
                await _functionMetadataManager.GetMetadataAsync(chainId, addrA.GetFormatted() + ".Func4"));

            Assert.Equal(new FunctionMetadata(
                    new HashSet<string>(new[]
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
                    })),
                await _functionMetadataManager.GetMetadataAsync(chainId, addrA.GetFormatted() + ".Func5"));

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

            Assert.Equal(callGraph, await _functionMetadataManager.GetCallGraphAsync(chainId));
        }
    }
    */
}