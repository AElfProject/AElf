using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.SmartContract;
using AElf.SmartContract.MetaData;
using AElf.Types.CSharp.MetadataAttribute;
using Google.Protobuf;
using QuickGraph;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Runtime.CSharp.Tests
{
    [UseAutofacTestFramework]
    public class RunnerMetadataTest
    {
        private IDataStore _store;
        private MockSetup _mock;

        public RunnerMetadataTest(IDataStore store, MockSetup mock)
        {
            _store = store;
            _mock = mock;
        }

        [Fact]
        public async Task TestTryAddNewContract()
        {
            var runner = new SmartContractRunner(new RunnerConfig()
            {
                SdkDir = _mock.SdkDir
            });
            
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(_mock.ContractCode),
                ContractHash = new Hash(_mock.ContractCode)
            };

            var resC = runner.ExtractMetadata(typeof(TestContractC));
            
            // Structure of the test data
            var groundTruthResC = new Dictionary<string, FunctionMetadataTemplate>(
                new[] // function metadata map for contract
                {
                    new KeyValuePair<string, FunctionMetadataTemplate>(
                        "${this}.Func0", //     local function name
                        new FunctionMetadataTemplate( //     local function metadata
                            new HashSet<string>(), //         calling set of this function metadata
                            new HashSet<Resource>(new[] //         local resource set of this function metadata
                            {
                                new Resource("${this}.resource4",
                                    DataAccessMode.AccountSpecific) //             resource of the local resource set
                            }))),

                    new KeyValuePair<string, FunctionMetadataTemplate>(
                        "${this}.Func1",
                        new FunctionMetadataTemplate(
                            new HashSet<string>(),
                            new HashSet<Resource>(new[]
                            {
                                new Resource("${this}.resource5",
                                    DataAccessMode.ReadOnlyAccountSharing)
                            })))
                });

            var localGraphC = new AdjacencyGraph<string, Edge<string>>();
            var vertexC = new[] {"${this}.Func0", "${this}.Func1"};
            localGraphC.AddVertexRange(vertexC);
            
            Assert.Equal("AElf.Runtime.CSharp.Tests.TestContractC", resC.FullName);
            Assert.Equal(groundTruthResC, resC.MethodMetadataTemplates);
            Assert.Equal(new Dictionary<string, Hash>(), resC.ContractReferences);
            Assert.Equal(localGraphC.Edges, resC.LocalCallingGraph.Edges);
            Assert.Equal(localGraphC.Vertices, resC.LocalCallingGraph.Vertices);
            Assert.Equal(new Dictionary<string, List<string>>(), resC.ExternalFuncCall);

            var resB = runner.ExtractMetadata(typeof(TestContractB));

            var groundTruthResB = new Dictionary<string, FunctionMetadataTemplate>(new[]
            {
                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func0",
                    new FunctionMetadataTemplate(
                        new HashSet<string>(new[] {"${ContractC}.Func1"}),
                        new HashSet<Resource>(new[]
                            {new Resource("${this}.resource2", DataAccessMode.AccountSpecific)}))),

                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func1",
                    new FunctionMetadataTemplate(
                        new HashSet<string>(),
                        new HashSet<Resource>(new[]
                            {new Resource("${this}.resource3", DataAccessMode.ReadOnlyAccountSharing)})))
            });
            
            var localGraphB = new AdjacencyGraph<string, Edge<string>>();
            var vertexB = new[] {"${this}.Func0", "${this}.Func1"};
            localGraphB.AddVertexRange(vertexB);
            var refB = new Dictionary<string, Hash>(new []
            {
                new KeyValuePair<string, Hash>("ContractC", ByteArrayHelpers.FromHexString("0x4567")), 
            });
            var externalB = new Dictionary<string, List<string>>(new []
            {
                new KeyValuePair<string, List<string>>("${this}.Func0", new List<string>(new []{"${ContractC}.Func1"})), 
            });
            
            Assert.Equal("AElf.Runtime.CSharp.Tests.TestContractB", resB.FullName);
            Assert.Equal(groundTruthResB.OrderBy(a=>a.Key), resB.MethodMetadataTemplates.OrderBy(a=>a.Key));
            Assert.Equal(refB.OrderBy(a=>a.Key), resB.ContractReferences.OrderBy(a=>a.Key));
            Assert.Equal(localGraphB, resB.LocalCallingGraph);
            Assert.Equal(externalB.OrderBy(a=>a.Key), resB.ExternalFuncCall.OrderBy(a=>a.Key));
            
            
            var resA = runner.ExtractMetadata(typeof(TestContractA));

            var groundTruthResA = new Dictionary<string, FunctionMetadataTemplate>(new[]
            {
                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func0(int)",
                    new FunctionMetadataTemplate(
                        new HashSet<string>(), new HashSet<Resource>())),

                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func0",
                    new FunctionMetadataTemplate(
                        new HashSet<string>(new[] {"${this}.Func1"}),
                        new HashSet<Resource>(new[]
                        {
                            new Resource("${this}.resource0", DataAccessMode.AccountSpecific)
                        }))),

                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func1",
                    new FunctionMetadataTemplate(
                        new HashSet<string>(new[] {"${this}.Func2"}),
                        new HashSet<Resource>(new[]
                        {
                            new Resource("${this}.resource1", DataAccessMode.ReadOnlyAccountSharing)
                        }))),

                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func2",
                    new FunctionMetadataTemplate(
                        new HashSet<string>(),
                        new HashSet<Resource>(new[]
                        {
                            new Resource("${this}.resource1", DataAccessMode.ReadOnlyAccountSharing),
                            new Resource("${this}.resource2", DataAccessMode.ReadWriteAccountSharing)
                        }))),

                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func3",
                    new FunctionMetadataTemplate(
                        new HashSet<string>(new[] {"${_contractB}.Func0", "${this}.Func0", "${ContractC}.Func0"}),
                        new HashSet<Resource>(new[]
                        {
                            new Resource("${this}.resource1", DataAccessMode.ReadOnlyAccountSharing)
                        }))),

                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func4",
                    new FunctionMetadataTemplate(
                        new HashSet<string>(new[] {"${this}.Func2", "${this}.Func2"}),
                        new HashSet<Resource>())),

                new KeyValuePair<string, FunctionMetadataTemplate>(
                    "${this}.Func5",
                    new FunctionMetadataTemplate(
                        new HashSet<string>(new[] {"${_contractB}.Func1", "${this}.Func3"}),
                        new HashSet<Resource>())),
            });
            
            var localGraphA = new AdjacencyGraph<string, Edge<string>>();
            var vertexA = new[] {"${this}.Func0(int)", "${this}.Func0", "${this}.Func1" ,"${this}.Func2","${this}.Func3","${this}.Func4","${this}.Func5",};
            var edgeA = new Edge<string>[]
            {
                new Edge<string>("${this}.Func0", "${this}.Func1"),
                new Edge<string>("${this}.Func1", "${this}.Func2"),
                new Edge<string>("${this}.Func3", "${this}.Func0"),
                new Edge<string>("${this}.Func4", "${this}.Func2"),
                new Edge<string>("${this}.Func5", "${this}.Func3"),
            };
            localGraphA.AddVertexRange(vertexA);
            localGraphA.AddEdgeRange(edgeA);
            var refA = new Dictionary<string, Hash>(new []
            {
                new KeyValuePair<string, Hash>("ContractC", ByteArrayHelpers.FromHexString("0x4567")), 
                new KeyValuePair<string, Hash>("_contractB", ByteArrayHelpers.FromHexString("0x1234")), 
            });
            var externalA = new Dictionary<string, List<string>>(new []
            {
                new KeyValuePair<string, List<string>>("${this}.Func3", new List<string>(new []{"${_contractB}.Func0", "${ContractC}.Func0"})), 
                new KeyValuePair<string, List<string>>("${this}.Func5", new List<string>(new []{"${_contractB}.Func0"})), 
            });
            
            Assert.Equal("AElf.Runtime.CSharp.Tests.TestContractA", resA.FullName);
            Assert.Equal(groundTruthResA.OrderBy(a=>a.Key), resA.MethodMetadataTemplates.OrderBy(a=>a.Key));
            Assert.Equal(refA, resA.ContractReferences);
            Assert.Equal(localGraphA, resA.LocalCallingGraph);
            Assert.Equal(externalA, resA.ExternalFuncCall);

            //test fail cases
            await TestFailCases(runner);
        }
        
        public string FunctionMetadataTemplateMapToString(Dictionary<string, FunctionMetadataTemplate> map)
        {
            
            return string.Join(
                " ",
                map.OrderBy(a => a.Key)
                    .Select(item => String.Format("[{0},({1}),({2})]", 
                        item.Key,
                        CallingSetToString(item.Value.CallingSet), 
                        PathSetToString(item.Value.LocalResourceSet))));
        }

        public string ContractMetadataTemplateMapToString(
            Dictionary<string, Dictionary<string, FunctionMetadataTemplate>> map)
        {
            return string.Join(
                " | ",
                map.OrderBy(a => a.Key)
                    .Select(item =>
                        String.Format("({0} - {1})", item.Key, FunctionMetadataTemplateMapToString(item.Value))));
        }
        
        public string PathSetToString(HashSet<Resource> resourceSet)
        {
            return string.Join(", ",
                resourceSet.OrderBy(a =>a.Name));
        }

        public string CallingSetToString(HashSet<string> callingSet)
        {
            return string.Join(", ", callingSet.OrderBy(a => a));
        }


        public async Task TestFailCases(SmartContractRunner runner)
        {
            var groundTruthMap =
                new Dictionary<string, Dictionary<string, FunctionMetadataTemplate>>();

            var exception = await Assert
                .ThrowsAsync<FunctionMetadataException>(() => Task.FromResult(runner.ExtractMetadata(typeof(TestContractD))));

            Assert.True(exception.Message.Contains("Duplicate name of field attributes in contract"));

            exception = await Assert
                .ThrowsAsync<FunctionMetadataException>(() => Task.FromResult(runner.ExtractMetadata(typeof(TestContractE))));
            Assert.True(
                exception.Message.Contains("Duplicate name of smart contract reference attributes in contract "));

            exception = await Assert
                .ThrowsAsync<FunctionMetadataException>(() => Task.FromResult(runner.ExtractMetadata(typeof(TestContractF))));
            Assert.True(exception.Message.Contains("Unknown reference local field ${this}.resource1"));

            exception = await Assert
                .ThrowsAsync<FunctionMetadataException>(() => Task.FromResult(runner.ExtractMetadata(typeof(TestContractG))));
            Assert.True(exception.Message.Contains("Duplicate name of function attribute"));

            exception = await Assert
                .ThrowsAsync<FunctionMetadataException>(() => Task.FromResult(runner.ExtractMetadata(typeof(TestContractH))));
            Assert.True(exception.Message.Contains("contains unknown reference to it's own function"));

            exception = await Assert
                .ThrowsAsync<FunctionMetadataException>(() => Task.FromResult(runner.ExtractMetadata(typeof(TestContractI))));
            Assert.True(exception.Message.Contains("contains unknown local member reference to other contract"));

            exception = await Assert
                .ThrowsAsync<FunctionMetadataException>(() => Task.FromResult(runner.ExtractMetadata(typeof(TestContractJ))));
            Assert.True(exception.Message.Contains("is Non-DAG thus nothing take effect"));

            exception = await Assert
                .ThrowsAsync<FunctionMetadataException>(() => Task.FromResult(runner.ExtractMetadata(typeof(TestContractK))));
            Assert.True(
                exception.Message.Contains("consider the target function does not exist in the foreign contract"));
        }
    }

    #region Dummy Contract for test

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

        [SmartContractReference("ContractC", "0x4567")]
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

        [SmartContractReference("_contractB", "0x1234")]
        private TestContractB _contractB;

        [SmartContractReference("ContractC", "0x4567")]
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
        [SmartContractReference("_contractB", "0x3211")]
        private TestContractB _contractB;

        [SmartContractReference("_contractB", "0x3211")]
        public TestContractC ContractC;

        public void Func0()
        {
        }

        public void Func1()
        {
        }
    }

    internal class TestContractF
    {
        [SmartContractFieldData("${this}.resource0", DataAccessMode.AccountSpecific)]
        public int resource0;

        //unknown local field
        [SmartContractFunction("${this}.Func0", new string[] { }, new[] {"${this}.resource1"})]
        public void Func0()
        {
        }
    }

    internal class TestContractG
    {
        //duplicate function attribute
        [SmartContractFunction("${this}.Func0", new string[] { }, new string[] { })]
        public void Func0()
        {
        }

        [SmartContractFunction("${this}.Func0", new string[] { }, new string[] { })]
        public void Func1()
        {
        }
    }

    internal class TestContractH
    {
        //unknown local function reference
        [SmartContractFunction("${this}.Func0", new string[] {"${this}.Func1"}, new string[] { })]
        public void Func0()
        {
        }
    }

    internal class TestContractI
    {
        //unknown foreign function reference
        [SmartContractFunction("${this}.Func0", new string[] {"${_contractA}.Func1"}, new string[] { })]
        public void Func0()
        {
        }
    }

    internal class TestContractJ
    {
        //for Non-DAG
        [SmartContractFunction("${this}.Func0", new string[] {"${this}.Func1"}, new string[] { })]
        public void Func0()
        {
        }

        [SmartContractFunction("${this}.Func1", new string[] {"${this}.Func2"}, new string[] { })]
        public void Func1()
        {
        }

        [SmartContractFunction("${this}.Func2", new string[] {"${this}.Func3"}, new string[] { })]
        public void Func2()
        {
        }

        [SmartContractFunction("${this}.Func3", new string[] {"${this}.Func1"}, new string[] { })]
        public void Func3()
        {
        }
    }

    internal class TestContractK
    {
        [SmartContractReference("_contractB", "0x1234")]
        private TestContractB _contractB;

        //want to call foreign function that Not Exist
        [SmartContractFunction("${this}.Func0", new string[] {"${_contractB}.Func10"}, new string[] { })]
        public void Func0()
        {
        }
    }

    #endregion
}