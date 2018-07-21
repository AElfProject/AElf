using System.Collections.Generic;
using System.Threading.Tasks;
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
        private IFunctionMetadataService _functionMetadataService;

        public FunctionMetadataServiceTest(IFunctionMetadataService functionMetadataService)
        {
            _functionMetadataService = functionMetadataService;
        }

        /*
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
                addrC.ToHex() + ".Func0", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific)
                    })));
            
            groundTruthMap.Add(
                addrC.ToHex() + ".Func1", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing) 
                    })));
            
            groundTruthMap.Add(
                addrB.ToHex() + ".Func0",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrC.ToHex() + ".Func1"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                    })));
            
            groundTruthMap.Add(
                addrB.ToHex() + ".Func1",
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrB.Value.ToByteArray().ToHex() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                    })));
            
            groundTruthMap.Add(
                addrA.ToHex() + ".Func0(int)",
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>()));
            
            groundTruthMap.Add(
                addrA.ToHex() + ".Func0",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.ToHex() + ".Func1"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                addrA.ToHex() + ".Func1",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.ToHex() + ".Func2"
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                addrA.ToHex() + ".Func2",
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                addrA.ToHex() + ".Func3",
                new FunctionMetadata(
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
                    })));
            
            groundTruthMap.Add(
                addrA.ToHex() + ".Func4",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.ToHex() + ".Func2"
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                addrA.ToHex() + ".Func5",
                new FunctionMetadata(
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
                addrC.ToHex() + ".Func0", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToByteArray().ToHex() + ".resource4", DataAccessMode.AccountSpecific)
                    })));
            
            groundTruthMap.Add(
                addrC.ToHex() + ".Func1", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing) 
                    })));
            
            groundTruthMap.Add(
                addrB.ToHex() + ".Func0",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrC.ToHex() + ".Func1"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToByteArray().ToHex() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrB.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.AccountSpecific), 
                    })));
            
            groundTruthMap.Add(
                addrB.ToHex() + ".Func1",
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrB.Value.ToByteArray().ToHex() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                    })));
            
            groundTruthMap.Add(
                addrA.ToHex() + ".Func0(int)",
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>()));
            
            groundTruthMap.Add(
                addrA.ToHex() + ".Func0",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.ToHex() + ".Func1"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource0", DataAccessMode.AccountSpecific),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                addrA.ToHex() + ".Func1",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.ToHex() + ".Func2"
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                addrA.ToHex() + ".Func2",
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                addrA.ToHex() + ".Func3",
                new FunctionMetadata(
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
                    })));
            
            groundTruthMap.Add(
                addrA.ToHex() + ".Func4",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.ToHex() + ".Func2"
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToByteArray().ToHex() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                addrA.ToHex() + ".Func5",
                new FunctionMetadata(
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
                    })));
            
            
            groundTruthMap.Add(
                contract1Addr.ToHex() + ".Func1", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                contract1Addr.ToHex() + ".Func2", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                contract2Addr.ToHex() + ".Func1", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract2Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                contract2Addr.ToHex() + ".Func2", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract2Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                refContractAddr.ToHex() + ".Func1", 
                new FunctionMetadata(
                    new HashSet<string>(new []{contract1Addr.ToHex() + ".Func1"}),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                refContractAddr.ToHex() + ".Func1_1", 
                new FunctionMetadata(
                    new HashSet<string>(new []{contract1Addr.ToHex() + ".Func1"}),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                        new Resource(refContractAddr.Value.ToByteArray().ToHex() + ".localRes", DataAccessMode.AccountSpecific)
                    })));
            
            groundTruthMap.Add(
                refContractAddr.ToHex() + ".Func2", 
                new FunctionMetadata(
                    new HashSet<string>(new []{contract1Addr.ToHex() + ".Func2"}),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    })));
            
            groundTruthMap.Add(
                refContractAddr.ToHex() + ".Func2_1", 
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        contract1Addr.ToHex() + ".Func2",
                        refContractAddr.ToHex() + ".Func1_1"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                        new Resource(refContractAddr.Value.ToByteArray().ToHex() + ".localRes", DataAccessMode.AccountSpecific)
                    })));
            
            groundTruthMap.Add(
                refContractAddr.ToHex() + ".Func2_2", 
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        contract1Addr.ToHex() + ".Func2",
                        refContractAddr.ToHex() + ".Func1"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    })));
            
            groundTruthMap.Add(
                refContractAddr.ToHex() + ".Func3", 
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        contract1Addr.ToHex() + ".Func1",
                        contract1Addr.ToHex() + ".Func2"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    })));
            
            groundTruthMap.Add(
                refContractAddr.ToHex() + ".Func4", 
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        contract1Addr.ToHex() + ".Func1",
                        contract2Addr.ToHex() + ".Func1"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(contract1Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                        new Resource(contract2Addr.Value.ToByteArray().ToHex() + "._lock", DataAccessMode.ReadWriteAccountSharing),
                    })));
            
            groundTruthMap.Add(
                refContractAddr.ToHex() + ".Func5", 
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        contract1Addr.ToHex() + ".Func2",
                        addrC.ToHex() + ".Func0"
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
    
    */
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

        [SmartContractReference("ContractC", "0x38f3a6b010cadfa690cc1900241c053f751c35f7c681be14bcee03085dc8a309")]
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

        [SmartContractReference("_contractB", "0x22e7340eb68c9a01804aabeb10c2ea0e38633e550dc4b7914010c2d7c95086ee")]
        private TestContractB _contractB;

        [SmartContractReference("ContractC", "0x38f3a6b010cadfa690cc1900241c053f751c35f7c681be14bcee03085dc8a309")]
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
        [SmartContractReference("ref1", "0x123")]
        public TestNonAttrContract1 ref1;
        
        [SmartContractReference("ref2", "0x456")]
        public TestNonAttrContract2 ref2;

        [SmartContractReference("refc", "0x789")]
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