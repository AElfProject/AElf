using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency.Metadata;
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
        private IFunctionMetadataService _functionMetadataService;

        public FunctionMetadataServiceTest(IFunctionMetadataService functionMetadataService)
        {
            _functionMetadataService = functionMetadataService;
        }

        [Fact]
        public async Task TestDepolyContract()
        {   
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
                    })));
            
            groundTruthMap.Add(
                addrC.Value.ToByteArray().ToHex() + ".Func1", 
                new FunctionMetadata(
                    new HashSet<string>(),
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
                    })));
            
            groundTruthMap.Add(
                addrB.Value.ToByteArray().ToHex() + ".Func1",
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrB.Value.ToByteArray().ToHex() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                    })));
            
            groundTruthMap.Add(
                addrA.Value.ToByteArray().ToHex() + ".Func0(int)",
                new FunctionMetadata(
                    new HashSet<string>(),
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
                    })));
            
            groundTruthMap.Add(
                addrA.Value.ToByteArray().ToHex() + ".Func2",
                new FunctionMetadata(
                    new HashSet<string>(),
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
                    })));
            
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
                    })));

            foreach (var kv in groundTruthMap)
            {
                Assert.Equal(kv.Value, _functionMetadataService.GetFunctionMetadata(chainId, kv.Key));
            }
        }
        
        [Fact]
        public async Task TestEmptyContract()
        {
            var chainId = Hash.Generate();
            var contract1Addr = new Hash("contract1".CalculateHash()).ToAccount();
            var contract2Addr = new Hash("contract2".CalculateHash()).ToAccount();
            var refContractAddr = new Hash("ref".CalculateHash()).ToAccount();
            
            var addrA = new Hash("TestContractA".CalculateHash());
            var addrB = new Hash("TestContractB".CalculateHash());
            var addrC = new Hash("TestContractC".CalculateHash());

            var referenceBookForA = new Dictionary<string, Hash>();
            referenceBookForA.Add("ContractC", addrC);
            referenceBookForA.Add("_contractB", addrB);
            var referenceBookForB = new Dictionary<string, Hash>();
            referenceBookForB.Add("ContractC", addrC);
            var referenceBookForC = new Dictionary<string, Hash>();
            var referenceBookForEmptyRef = new Dictionary<string, Hash>();
            referenceBookForEmptyRef.Add("refc", addrC);
            referenceBookForEmptyRef.Add("ref1", contract1Addr);
            referenceBookForEmptyRef.Add("ref2", contract2Addr);

            await _functionMetadataService.DeployContract(chainId, typeof(TestContractC), addrC, referenceBookForC);
            await _functionMetadataService.DeployContract(chainId, typeof(TestContractB), addrB, referenceBookForB);
            
            await _functionMetadataService.DeployContract(chainId, typeof(TestNonAttrContract1), contract1Addr,
                new Dictionary<string, Hash>());
            
            await _functionMetadataService.DeployContract(chainId, typeof(TestNonAttrContract2), contract2Addr,
                new Dictionary<string, Hash>());
            
            await _functionMetadataService.DeployContract(chainId, typeof(TestContractA), addrA, referenceBookForA);

            await _functionMetadataService.DeployContract(chainId, typeof(TestRefNonAttrContract), refContractAddr, referenceBookForEmptyRef);

            var groundTruthMap = new Dictionary<string, FunctionMetadata>();
            
            groundTruthMap.Add(
                addrC.Value.ToByteArray().ToHex() + ".Func0", 
                new FunctionMetadata(
                    new HashSet<string>(),
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
                    })));
            
            groundTruthMap.Add(
                addrB.Value.ToByteArray().ToHex() + ".Func1",
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrB.Value.ToByteArray().ToHex() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                    })));
            
            groundTruthMap.Add(
                addrA.Value.ToByteArray().ToHex() + ".Func0(int)",
                new FunctionMetadata(
                    new HashSet<string>(),
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
                    })));
            
            groundTruthMap.Add(
                addrA.Value.ToByteArray().ToHex() + ".Func2",
                new FunctionMetadata(
                    new HashSet<string>(),
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
                    })));
            
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
                    })));
            
            
            groundTruthMap.Add(
                contract1Addr.Value.ToByteArray().ToHex() + ".Func1", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                contract1Addr.Value.ToByteArray().ToHex() + ".Func2", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                contract2Addr.Value.ToByteArray().ToHex() + ".Func1", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract2Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                contract2Addr.Value.ToByteArray().ToHex() + ".Func2", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract2Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                refContractAddr.Value.ToByteArray().ToHex() + ".Func1", 
                new FunctionMetadata(
                    new HashSet<string>(new []{contract1Addr.Value.ToByteArray().ToHex() + ".Func1"}),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)})));
            
            groundTruthMap.Add(
                refContractAddr.Value.ToByteArray().ToHex() + ".Func1_1", 
                new FunctionMetadata(
                    new HashSet<string>(new []{contract1Addr.Value.ToByteArray().ToHex() + ".Func1"}),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                        new Resource(refContractAddr.Value.ToByteArray().ToHex() + ".localRes", DataAccessMode.AccountSpecific)
                    })));
            
            groundTruthMap.Add(
                refContractAddr.Value.ToByteArray().ToHex() + ".Func2", 
                new FunctionMetadata(
                    new HashSet<string>(new []{contract1Addr.Value.ToByteArray().ToHex() + ".Func2"}),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    })));
            
            groundTruthMap.Add(
                refContractAddr.Value.ToByteArray().ToHex() + ".Func2_1", 
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        contract1Addr.Value.ToByteArray().ToHex() + ".Func2",
                        refContractAddr.Value.ToByteArray().ToHex() + ".Func1_1"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                        new Resource(refContractAddr.Value.ToByteArray().ToHex() + ".localRes", DataAccessMode.AccountSpecific)
                    })));
            
            groundTruthMap.Add(
                refContractAddr.Value.ToByteArray().ToHex() + ".Func2_2", 
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        contract1Addr.Value.ToByteArray().ToHex() + ".Func2",
                        refContractAddr.Value.ToByteArray().ToHex() + ".Func1"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    })));
            
            groundTruthMap.Add(
                refContractAddr.Value.ToByteArray().ToHex() + ".Func3", 
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        contract1Addr.Value.ToByteArray().ToHex() + ".Func1",
                        contract1Addr.Value.ToByteArray().ToHex() + ".Func2"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    })));
            
            groundTruthMap.Add(
                refContractAddr.Value.ToByteArray().ToHex() + ".Func4", 
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        contract1Addr.Value.ToByteArray().ToHex() + ".Func1",
                        contract2Addr.Value.ToByteArray().ToHex() + ".Func1"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                        new Resource(contract2Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    })));
            
            groundTruthMap.Add(
                refContractAddr.Value.ToByteArray().ToHex() + ".Func5", 
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        contract1Addr.Value.ToByteArray().ToHex() + ".Func2",
                        addrC.Value.ToByteArray().ToHex() + ".Func0"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                        new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific),
                    })));
            

            foreach (var kv in groundTruthMap)
            {
                Assert.Equal(kv.Value, _functionMetadataService.GetFunctionMetadata(chainId, kv.Key));
            }
        }
    }

    public class TestRefNonAttrContract : CSharpSmartContract
    {
        [SmartContractReference("ref1", typeof(TestNonAttrContract1))]
        public TestNonAttrContract1 ref1;
        
        [SmartContractReference("ref2", typeof(TestNonAttrContract2))]
        public TestNonAttrContract2 ref2;

        [SmartContractReference("refc", typeof(TestContractC))]
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