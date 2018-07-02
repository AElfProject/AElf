using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Tests.Concurrency.Scheduling;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.Concurrency.Metadata
{
    [UseAutofacTestFramework]
    public class FunctionMetadataServiceTest
    {
        private IFunctionMetadataService _functionMetadataService;

        public FunctionMetadataServiceTest(IFunctionMetadataService functionMetadataService)
        {
            _functionMetadataService = functionMetadataService;
        }

        [Fact]
        public async Task TestDepolyContract()
        {
            ParallelTestDataUtil util = new ParallelTestDataUtil();
            
            Hash chainId = Hash.Generate();
            var addrA = new Hash("TestContractA".CalculateHash());
            var addrB = new Hash("TestContractB".CalculateHash());
            var addrC = new Hash("TestContractC".CalculateHash());

            var referenceBookForA = new Dictionary<string, Hash>();
            referenceBookForA.Add("ContractC", addrC);
            referenceBookForA.Add("_contractB", addrB);
            var referenceBookForB = new Dictionary<string, Hash>();
            referenceBookForB.Add("ContractC", addrC);
            var referenceBookForC = new Dictionary<string, Hash>();

            await _functionMetadataService.DeployContract(chainId, typeof(TestContractC), addrC, referenceBookForC);
            await _functionMetadataService.DeployContract(chainId, typeof(TestContractB), addrB, referenceBookForB);
            await _functionMetadataService.DeployContract(chainId, typeof(TestContractA), addrA, referenceBookForA);

            var groundTruthMap = new Dictionary<string, FunctionMetadata>();
            
            groundTruthMap.Add(
                addrC.Value.ToByteArray().ToHex() + ".Func0", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific)
                    }), 
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific)
                    })));
            
            groundTruthMap.Add(
                addrC.Value.ToByteArray().ToHex() + ".Func1", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing) 
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing) 
                    })));
            
            groundTruthMap.Add(
                addrB.Value.ToByteArray().ToHex() + ".Func0",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrC.Value.ToByteArray().ToHex() + ".Func1"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                    })));
            
            groundTruthMap.Add(
                addrB.Value.ToByteArray().ToHex() + ".Func1",
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrB.Value.ToByteArray().ToHex() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrB.Value.ToByteArray().ToHex() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                    })));
            
            groundTruthMap.Add(
                addrA.Value.ToByteArray().ToHex() + ".Func0(int)",
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(),
                    new HashSet<Resource>()));
            
            groundTruthMap.Add(
                addrA.Value.ToByteArray().ToHex() + ".Func0",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.Value.ToByteArray().ToHex() + ".Func1"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                    })));
            
            groundTruthMap.Add(
                addrA.Value.ToByteArray().ToHex() + ".Func1",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.Value.ToByteArray().ToHex() + ".Func2"
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing)
                    })));
            
            groundTruthMap.Add(
                addrA.Value.ToByteArray().ToHex() + ".Func2",
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                addrA.Value.ToByteArray().ToHex() + ".Func3",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.Value.ToByteArray().ToHex() + ".Func0",
                        addrB.Value.ToByteArray().ToHex() + ".Func0", 
                        addrC.Value.ToByteArray().ToHex() + ".Func0"
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                        new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                        new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing)
                    })));
            
            groundTruthMap.Add(
                addrA.Value.ToByteArray().ToHex() + ".Func4",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.Value.ToByteArray().ToHex() + ".Func2"
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    }),
                    new HashSet<Resource>()));
            
            groundTruthMap.Add(
                addrA.Value.ToByteArray().ToHex() + ".Func5",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.Value.ToByteArray().ToHex() + ".Func3",
                        addrB.Value.ToByteArray().ToHex() + ".Func1"
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
                    }),
                    new HashSet<Resource>()));

            foreach (var kv in groundTruthMap)
            {
                Assert.Equal(util.FunctionMetadataToString(kv.Value), util.FunctionMetadataToString(_functionMetadataService.GetFunctionMetadata(chainId, kv.Key)));
            }
        }
    }
}