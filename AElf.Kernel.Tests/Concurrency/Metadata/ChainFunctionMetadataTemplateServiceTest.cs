using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Contracts.Examples;
using AElf.Kernel.Storages;
using AElf.Kernel.Tests.Concurrency.Scheduling;
using QuickGraph;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.Concurrency.Metadata
{
    [UseAutofacTestFramework]
    public class ChainFunctionMetadataTemplateServiceTest
    {
        private ParallelTestDataUtil util = new ParallelTestDataUtil();
        private IDataStore _dataStore;
        private Hash chainId;

        public ChainFunctionMetadataTemplateServiceTest(IDataStore dataStore, Hash chainId)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            this.chainId = chainId;
        }

        [Fact]
        public async Task<ChainFunctionMetadataTemplateService> TestTryAddNewContractShouldSuccess()
        {
            ChainFunctionMetadataTemplateService cfts = new ChainFunctionMetadataTemplateService(_dataStore, chainId);
            var groundTruthMap = new Dictionary<string, Dictionary<string, FunctionMetadataTemplate>> (cfts.ContractMetadataTemplateMap);
            //Throw exception because 
            var exception = Assert.ThrowsAsync<FunctionMetadataException>(() => cfts.TryAddNewContract(typeof(TestContractA))).Result;
            Assert.True(exception.Message.Contains("Unknow reference of the foreign target"));
            
            //Not changed
            Assert.Equal(util.ContractMetadataTemplateMapToString(groundTruthMap), util.ContractMetadataTemplateMapToString(cfts.ContractMetadataTemplateMap));


            await cfts.TryAddNewContract(typeof(TestContractC));
                                                                                                  // Structure of the test data
            groundTruthMap.Add(
                "TestContractC",                                                                  // Contract name
                new Dictionary<string, FunctionMetadataTemplate>(new[]                            // function metadata map for contract
                {
                    new KeyValuePair<string, FunctionMetadataTemplate>(
                        "${this}.Func0()",                                                        //     local function name
                        new FunctionMetadataTemplate(                                             //     local function metadata
                            new HashSet<string>(),                                                //         calling set of this function metadata
                            new HashSet<Resource>(new[]                                           //         local resource set of this function metadata
                            {
                                new Resource("${this}.resource4", DataAccessMode.AccountSpecific) //             resource of the local resource set
                                    
                            }))),

                    new KeyValuePair<string, FunctionMetadataTemplate>(
                        "${this}.Func1()",
                        new FunctionMetadataTemplate(
                            new HashSet<string>(),
                            new HashSet<Resource>(new[]
                            {
                                new Resource("${this}.resource5",
                                    DataAccessMode.ReadOnlyAccountSharing)
                            })))
                }));
            
            Assert.Equal(util.ContractMetadataTemplateMapToString(groundTruthMap), util.ContractMetadataTemplateMapToString(cfts.ContractMetadataTemplateMap));

            await cfts.TryAddNewContract(typeof(TestContractB));

            groundTruthMap.Add("TestContractB", new Dictionary<string, FunctionMetadataTemplate>(new[]
            {
                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func0()", 
                    new FunctionMetadataTemplate(
                        new HashSet<string>(new[] {"${ContractC}.Func1()"}),
                        new HashSet<Resource>(new[] {new Resource("${this}.resource2", DataAccessMode.AccountSpecific)}))),
                
                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func1()", 
                    new FunctionMetadataTemplate(
                        new HashSet<string>(),
                        new HashSet<Resource>(new[] {new Resource("${this}.resource3", DataAccessMode.ReadOnlyAccountSharing)})))
            }));
            
            Assert.Equal(util.ContractMetadataTemplateMapToString(groundTruthMap), util.ContractMetadataTemplateMapToString(cfts.ContractMetadataTemplateMap));

            await cfts.TryAddNewContract(typeof(TestContractA));

            groundTruthMap.Add("TestContractA", new Dictionary<string, FunctionMetadataTemplate>(new[]
            {
                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func0(int)", 
                    new FunctionMetadataTemplate(
                        new HashSet<string>(), new HashSet<Resource>())),

                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func0()", 
                    new FunctionMetadataTemplate(
                        new HashSet<string>(new[] {"${this}.Func1()"}),
                        new HashSet<Resource>(new[]
                        {
                            new Resource("${this}.resource0", DataAccessMode.AccountSpecific)
                        }))),

                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func1()", 
                    new FunctionMetadataTemplate(
                        new HashSet<string>(new[] {"${this}.Func2()"}),
                        new HashSet<Resource>(new[]
                        {
                            new Resource("${this}.resource1", DataAccessMode.ReadOnlyAccountSharing)
                        }))),

                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func2()", 
                    new FunctionMetadataTemplate(
                        new HashSet<string>(),
                        new HashSet<Resource>(new[]
                        {
                            new Resource("${this}.resource1", DataAccessMode.ReadOnlyAccountSharing),
                            new Resource("${this}.resource2", DataAccessMode.ReadWriteAccountSharing)
                        }))),

                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func3()", 
                    new FunctionMetadataTemplate(
                        new HashSet<string>(new[] {"${_contractB}.Func0()", "${this}.Func0()", "${ContractC}.Func0()"}),
                        new HashSet<Resource>(new[]
                        {
                            new Resource("${this}.resource1", DataAccessMode.ReadOnlyAccountSharing)
                        }))),

                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func4()", 
                    new FunctionMetadataTemplate(
                        new HashSet<string>(new[] {"${this}.Func2()", "${this}.Func2()"}),
                        new HashSet<Resource>())),

                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func5()", 
                    new FunctionMetadataTemplate(
                        new HashSet<string>(new[] {"${_contractB}.Func1()", "${this}.Func3()"}),
                        new HashSet<Resource>())),
            }));
            
            Assert.Equal(util.ContractMetadataTemplateMapToString(groundTruthMap), util.ContractMetadataTemplateMapToString(cfts.ContractMetadataTemplateMap));
            
            //test fail cases
            await TestFailCases(cfts);
            
            //test restore
            ChainFunctionMetadataTemplateService newCFTS = new ChainFunctionMetadataTemplateService(_dataStore, chainId);    
            Assert.Equal(util.ContractMetadataTemplateMapToString(cfts.ContractMetadataTemplateMap), util.ContractMetadataTemplateMapToString(newCFTS.ContractMetadataTemplateMap));
            
            return cfts;
        }


        public async Task<ChainFunctionMetadataTemplateService> TestFailCases(ChainFunctionMetadataTemplateService cfts)
        {
            var groundTruthMap = new Dictionary<string, Dictionary<string, FunctionMetadataTemplate>>(cfts.ContractMetadataTemplateMap);
            
            var exception = Assert.ThrowsAsync<FunctionMetadataException>(()=> cfts.TryAddNewContract(typeof(TestContractD))).Result;
            
            Assert.True(exception.Message.Contains("Duplicate name of field attributes in contract"));

            exception = Assert.ThrowsAsync<FunctionMetadataException>(() => cfts.TryAddNewContract(typeof(TestContractE))).Result;
            Assert.True(exception.Message.Contains("Duplicate name of smart contract reference attributes in contract "));

            exception = Assert.ThrowsAsync<FunctionMetadataException>(() => cfts.TryAddNewContract(typeof(TestContractF))).Result;
            Assert.True(exception.Message.Contains("Unknown reference local field ${this}.resource1"));

            exception = Assert.ThrowsAsync<FunctionMetadataException>(() => cfts.TryAddNewContract(typeof(TestContractG))).Result;
            Assert.True(exception.Message.Contains("Duplicate name of function attribute"));
            
            exception = Assert.ThrowsAsync<FunctionMetadataException>(() => cfts.TryAddNewContract(typeof(TestContractH))).Result;
            Assert.True(exception.Message.Contains("contains unknown reference to it's own function"));
            
            exception = Assert.ThrowsAsync<FunctionMetadataException>(() => cfts.TryAddNewContract(typeof(TestContractI))).Result;
            Assert.True(exception.Message.Contains("contains unknown local member reference to other contract"));
            
            exception = Assert.ThrowsAsync<FunctionMetadataException>(() => cfts.TryAddNewContract(typeof(TestContractJ))).Result;
            Assert.True(exception.Message.Contains("is Non-DAG thus nothing take effect"));
            
            exception = Assert.ThrowsAsync<FunctionMetadataException>(() => cfts.TryAddNewContract(typeof(TestContractK))).Result;
            Assert.True(exception.Message.Contains("consider the target function does not exist in the foreign contract"));
            
            Assert.Equal(util.ContractMetadataTemplateMapToString(groundTruthMap), util.ContractMetadataTemplateMapToString(cfts.ContractMetadataTemplateMap));
            return cfts;
        }
    }

    #region Dummy Contract for test
    
    internal class TestContractC
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

    internal class TestContractB
    {
        [SmartContractFieldData("${this}.resource2", DataAccessMode.AccountSpecific)]
        public int resource2;
        [SmartContractFieldData("${this}.resource3", DataAccessMode.ReadOnlyAccountSharing)]
        private int resource3;

        [SmartContractReference("ContractC", typeof(TestContractC))]
        public TestContractC ContractC;
        
        [SmartContractFunction("${this}.Func0()", new []{"${ContractC}.Func1()"}, new []{"${this}.resource2"})]
        public void Func0(){}
        
        [SmartContractFunction("${this}.Func1()", new string[]{}, new []{"${this}.resource3"})]
        public void Func1(){}
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
        [SmartContractFunction("${this}.Func5()", new []{"${_contractB}.Func1()", "${this}.Func3()"}, new string[]{})]
        public void Func5(){}
    }
    
    
    
    //wrong cases
    internal class TestContractD
    {
        //duplicate field name
        [SmartContractFieldData("${this}.resource0", DataAccessMode.AccountSpecific)]
        public int resource0;
        [SmartContractFieldData("${this}.resource0", DataAccessMode.AccountSpecific)]
        public int resource1;
    }
    
    internal class TestContractE
    {
        
        [SmartContractFieldData("${this}.resource0", DataAccessMode.AccountSpecific)]
        public int resource0;
        [SmartContractFieldData("${this}.resource1", DataAccessMode.AccountSpecific)]
        public int resource1;
        
        //duplicate contract reference name
        [SmartContractReference("_contractB", typeof(TestContractB))]
        private TestContractB _contractB;

        [SmartContractReference("_contractB", typeof(TestContractC))]
        public TestContractC ContractC;
        
        public void Func0(){}
        public void Func1(){}
    }
    
    internal class TestContractF
    {
        [SmartContractFieldData("${this}.resource0", DataAccessMode.AccountSpecific)]
        public int resource0;
        
        //unknown local field
        [SmartContractFunction("${this}.Func0()", new string[]{}, new []{"${this}.resource1"})]
        public void Func0(){}
    }
    
    internal class TestContractG
    {
        //duplicate function attribute
        [SmartContractFunction("${this}.Func0()", new string[]{}, new string[]{})]
        public void Func0(){}
        
        [SmartContractFunction("${this}.Func0()", new string[]{}, new string[]{})]
        public void Func1(){}
    }
    
    internal class TestContractH
    {
        //unknown local function reference
        [SmartContractFunction("${this}.Func0()", new string[]{"${this}.Func1()"}, new string[]{})]
        public void Func0(){}
    }
    
    internal class TestContractI
    {
        //unknown foreign function reference
        [SmartContractFunction("${this}.Func0()", new string[]{"${_contractA}.Func1()"}, new string[]{})]
        public void Func0(){}
    }

    internal class TestContractJ
    {
        //for Non-DAG
        [SmartContractFunction("${this}.Func0()", new string[]{"${this}.Func1()"}, new string[]{})]
        public void Func0(){}
        [SmartContractFunction("${this}.Func1()", new string[]{"${this}.Func2()"}, new string[]{})]
        public void Func1(){}
        [SmartContractFunction("${this}.Func2()", new string[]{"${this}.Func3()"}, new string[]{})]
        public void Func2(){}
        [SmartContractFunction("${this}.Func3()", new string[]{"${this}.Func1()"}, new string[]{})]
        public void Func3(){}
    }
    
    internal class TestContractK
    {
        [SmartContractReference("_contractB", typeof(TestContractB))]
        private TestContractB _contractB;
        //want to call foreign function that Not Exist
        [SmartContractFunction("${this}.Func0()", new string[]{"${_contractB}.Func10()"}, new string[]{})]
        public void Func0(){}
    }

    #endregion
    
}