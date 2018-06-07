using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Contracts.Examples;
using AElf.Kernel.Tests.Concurrency.Scheduling;
using QuickGraph;
using Xunit;

namespace AElf.Kernel.Tests.Concurrency.Metadata
{
    public class ChainFunctionMetadataTemplateServiceTest
    {
        [Fact]
        public void TestTryAddNewContract()
        {
            ParallelTestDataUtil util = new ParallelTestDataUtil();
            ChainFunctionMetadataTemplateService cfts = new ChainFunctionMetadataTemplateService();
            var groundTruthMap = new Dictionary<string, FunctionMetadataTemplate>(cfts.FunctionMetadataTemplateMap);
            //Throw exception because 
            Assert.Throws<FunctionMetadataException>(() => { cfts.TryAddNewContract(typeof(TestContractA)); });
            //Not changed
            Assert.Equal(util.FunctionMetadataTemplateMapToString(groundTruthMap), util.FunctionMetadataTemplateMapToString(cfts.FunctionMetadataTemplateMap));


            cfts.TryAddNewContract(typeof(TestContractC));
            
            groundTruthMap.Add("TestContractC.Func0()", new FunctionMetadataTemplate(
                new HashSet<string>(), 
                new HashSet<Resource>(new [] {new Resource("${this}.resource4",
                    DataAccessMode.AccountSpecific) })));
            groundTruthMap.Add("TestContractC.Func1()", new FunctionMetadataTemplate(
                new HashSet<string>(), 
                new HashSet<Resource>(new [] {new Resource("${this}.resource5",
                    DataAccessMode.ReadOnlyAccountSharing) })));
            
            Assert.Equal(util.FunctionMetadataTemplateMapToString(groundTruthMap), util.FunctionMetadataTemplateMapToString(cfts.FunctionMetadataTemplateMap));

            cfts.TryAddNewContract(typeof(TestContractB));
            
            groundTruthMap.Add("TestContractB.Func0()", new FunctionMetadataTemplate(
                new HashSet<string>(new []{"${ContractC}.Func1()"}), 
                new HashSet<Resource>(new []{new Resource("${this}.resource2", DataAccessMode.AccountSpecific) })));
            Assert.Equal(util.FunctionMetadataTemplateMapToString(groundTruthMap), util.FunctionMetadataTemplateMapToString(cfts.FunctionMetadataTemplateMap));

            cfts.TryAddNewContract(typeof(TestContractA));
            
            groundTruthMap.Add("TestContractA.Func0(int)", new FunctionMetadataTemplate(
                new HashSet<string>(), new HashSet<Resource>()));
            groundTruthMap.Add("TestContractA.Func0()", new FunctionMetadataTemplate(
                new HashSet<string>(new []{"${this}.Func1()"}), 
                new HashSet<Resource>(new []
                {
                    new Resource("${this}.resource0", DataAccessMode.AccountSpecific),
                    new Resource("${this}.resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource("${this}.resource2", DataAccessMode.ReadWriteAccountSharing)
                })));
            
            groundTruthMap.Add("TestContractA.Func1()", new FunctionMetadataTemplate(
                new HashSet<string>(new []{"${this}.Func2()"}), 
                new HashSet<Resource>(new []
                {
                    new Resource("${this}.resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource("${this}.resource2", DataAccessMode.ReadWriteAccountSharing)
                })));

            groundTruthMap.Add("TestContractA.Func2()", new FunctionMetadataTemplate(
                new HashSet<string>(),
                new HashSet<Resource>(new[]
                {
                    new Resource("${this}.resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource("${this}.resource2", DataAccessMode.ReadWriteAccountSharing)
                })));
            
            groundTruthMap.Add("TestContractA.Func3()", new FunctionMetadataTemplate(
                new HashSet<string>(new []{"${_contractB}.Func0()", "${this}.Func0()", "${ContractC}.Func0()"}), 
                new HashSet<Resource>(new []
                {
                    new Resource("${this}.resource0", DataAccessMode.AccountSpecific),
                    new Resource("${this}.resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource("${this}.resource2", DataAccessMode.ReadWriteAccountSharing)
                })));
            groundTruthMap.Add("TestContractA.Func4()", new FunctionMetadataTemplate(
                new HashSet<string>(new []{"${this}.Func2()", "${this}.Func2()"}), 
                new HashSet<Resource>(new []
                {
                    new Resource("${this}.resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource("${this}.resource2", DataAccessMode.ReadWriteAccountSharing)
                })));
            groundTruthMap.Add("TestContractA.Func5()", new FunctionMetadataTemplate(
                new HashSet<string>(new []{"${_contractB}.Func0()", "${this}.Func3()"}), 
                new HashSet<Resource>(new []
                {
                    new Resource("${this}.resource0", DataAccessMode.AccountSpecific),
                    new Resource("${this}.resource1", DataAccessMode.ReadOnlyAccountSharing),
                    new Resource("${this}.resource2", DataAccessMode.ReadWriteAccountSharing)
                })));
            
            Assert.Equal(util.FunctionMetadataTemplateMapToString(groundTruthMap), util.FunctionMetadataTemplateMapToString(cfts.FunctionMetadataTemplateMap));
        }
    }

    #region Dummy Contract for test
    
    public class TestContractC
    {
        [SmartContractFieldData("${this}.resource4", DataAccessMode.AccountSpecific)]
        public int resource4;
        [SmartContractFieldData("${this}.resource5", DataAccessMode.ReadOnlyAccountSharing)]
        private int resource5;
        
        [SmartContractFunction("${this}.Func0()", new string[]{}, new []{"${this}.resource4"})]
        public void Func0(){}
        [SmartContractFunction("${this}.Func1()", new string[]{}, new []{"${this}.resource5"})]
        public void Func1(){}
    }

    public class TestContractB
    {
        [SmartContractFieldData("${this}.resource2", DataAccessMode.AccountSpecific)]
        public int resource2;
        [SmartContractFieldData("${this}.resource3", DataAccessMode.ReadOnlyAccountSharing)]
        private int resource3;

        [SmartContractReference("ContractC", typeof(TestContractC))]
        public TestContractC ContractC;
        
        [SmartContractFunction("${this}.Func0()", new []{"${ContractC}.Func1()"}, new []{"${this}.resource2"})]
        public void Func0(){}
    }
    
    public class TestContractA
    {
        //test for different accessibility
        [SmartContractFieldData("${this}.resource0", DataAccessMode.AccountSpecific)]
        public int resource0;
        [SmartContractFieldData("${this}.resource1", DataAccessMode.ReadOnlyAccountSharing)]
        private int resource1;
        [SmartContractFieldData("${this}.resource2", DataAccessMode.ReadWriteAccountSharing)]
        protected int resource2;

        [SmartContractReference("_contractB", typeof(TestContractB))]
        private TestContractB _contractB;

        [SmartContractReference("ContractC", typeof(TestContractC))]
        public TestContractC ContractC;
        
        
        //test for empty calling set and resource set
        [SmartContractFunction("${this}.Func0(int)", new string[]{}, new string[]{})]
        private void Func0(int a){}
        
        
        //test for same func name but different parameter
        //test for local function references recursive (resource completation)
        [SmartContractFunction("${this}.Func0()", new []{"${this}.Func1()"}, new string[]{"${this}.resource0"})]
        public void Func0(){}
        
        //test for local function reference non-recursive and test for overlap resource set
        [SmartContractFunction("${this}.Func1()", new []{"${this}.Func2()"}, new []{"${this}.resource1"})]
        public void Func1(){}
        
        //test for foreign contract, test for duplicate local resource
        //when deploy: test for recursive foreign resource collect
        [SmartContractFunction("${this}.Func2()", new string[]{}, new []{"${this}.resource1", "${this}.resource2"})]
        protected void Func2(){}
        
        //test for foreign calling set only
        [SmartContractFunction("${this}.Func3()", new []{"${_contractB}.Func0()", "${this}.Func0()", "${ContractC}.Func0()"}, new []{"${this}.resource1"})]
        public void Func3(){}
        
        //test for duplication in calling set
        [SmartContractFunction("${this}.Func4()", new []{"${this}.Func2()", "${this}.Func2()"}, new string[]{})]
        public void Func4(){}
        
        //test for duplicate foreign call
        [SmartContractFunction("${this}.Func5()", new []{"${_contractB}.Func0()", "${this}.Func3()"}, new string[]{})]
        public void Func5(){}
    }

    #endregion
    
}