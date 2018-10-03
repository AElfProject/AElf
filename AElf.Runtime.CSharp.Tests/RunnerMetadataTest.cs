using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration.Config.Contract;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.SmartContract;
using AElf.SmartContract.MetaData;
using AElf.Types.CSharp.MetadataAttribute;
using Google.Protobuf;
using QuickGraph;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Common;

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
            RunnerConfig.Instance.SdkDir = _mock.SdkDir;
            var runner = new SmartContractRunner();
            
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(_mock.ContractCode),
                ContractHash = Hash.FromBytes(_mock.ContractCode)
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
            
            var groundTruthTemplateC = new ContractMetadataTemplate(typeof(TestContractC).FullName, groundTruthResC,
                new Dictionary<string, Address>());
            
            Assert.Equal(groundTruthTemplateC, resC, new ContractMetadataTemplateEqualityComparer());

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
            
            var refB = new Dictionary<string, Address>(new []
            {
                new KeyValuePair<string, Address>("ContractC", Address.FromBytes(ByteArrayHelpers.FromHexString("0x456745674567456745674567456745674567"))), 
            });
            
            var groundTruthTemplateB = new ContractMetadataTemplate(typeof(TestContractB).FullName, groundTruthResB,
                refB);
            
            Assert.Equal(groundTruthTemplateB, resB, new ContractMetadataTemplateEqualityComparer());
            
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
            
            var refA = new Dictionary<string, Address>(new []
            {
                new KeyValuePair<string, Address>("ContractC", Address.FromBytes(ByteArrayHelpers.FromHexString("0x456745674567456745674567456745674567"))), 
                new KeyValuePair<string, Address>("_contractB", Address.FromBytes(ByteArrayHelpers.FromHexString("0x123412341234123412341234123412341234"))), 
            });
            
            var groundTruthTemplateA = new ContractMetadataTemplate(typeof(TestContractA).FullName, groundTruthResA,
                refA);
            
            Assert.Equal(groundTruthTemplateA, resA, new ContractMetadataTemplateEqualityComparer());

            //test fail cases
            await TestFailCases(runner);
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
        }
    }

    internal sealed class ContractMetadataTemplateEqualityComparer : IEqualityComparer<ContractMetadataTemplate>
    {
        public bool Equals(ContractMetadataTemplate x, ContractMetadataTemplate y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;

            return CallingGraphToString(x.LocalCallingGraph) == CallingGraphToString(y.LocalCallingGraph) &&
                   FunctionMetadataTemplateMapToString(x.MethodMetadataTemplates) ==
                   FunctionMetadataTemplateMapToString(y.MethodMetadataTemplates) &&
                   string.Equals(x.FullName, y.FullName) &&
                   ContractReferencesToString(x.ContractReferences) ==
                   ContractReferencesToString(y.ContractReferences) &&
                   ExternalFunctionCallToString(x.ExternalFuncCall) == ExternalFunctionCallToString(y.ExternalFuncCall);
        }

        public int GetHashCode(ContractMetadataTemplate obj)
        {
            unchecked
            {
                var hashCode = (obj.LocalCallingGraph != null ? obj.LocalCallingGraph.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^
                           (obj.MethodMetadataTemplates != null ? obj.MethodMetadataTemplates.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.FullName != null ? obj.FullName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^
                           (obj.ContractReferences != null ? obj.ContractReferences.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.ExternalFuncCall != null ? obj.ExternalFuncCall.GetHashCode() : 0);
                return hashCode;
            }
        }

        private string CallingGraphToString(AdjacencyGraph<string, Edge<string>> callGraph)
        {
            return string.Join(", ", callGraph.Edges?.OrderBy(a => a.Source).ThenBy(a => a.Target).Select(a => a.ToString())) +
                   string.Join(", ", callGraph.Vertices?.OrderBy(a => a));
        }

        private string ContractReferencesToString(Dictionary<string, Address> references)
        {
            return string.Join(", ", references.OrderBy(kv => kv.Key).Select(kv => $"[{kv.Key}, {kv.Value.Dumps()}]"));
        }

        private string ExternalFunctionCallToString(Dictionary<string, List<string>> externalFuncCall)
        {
            return string.Join(", ",
                externalFuncCall.OrderBy(kv => kv.Key)
                    .Select(kv => $"[{kv.Key}, ({string.Join(", ", kv.Value)})]"));
        }

        private string FunctionMetadataTemplateMapToString(Dictionary<string, FunctionMetadataTemplate> map)
        {

            return string.Join(
                " ",
                map.OrderBy(a => a.Key)
                    .Select(item => String.Format("[{0},({1}),({2})]",
                        item.Key,
                        CallingSetToString(item.Value.CallingSet),
                        PathSetToString(item.Value.LocalResourceSet))));
        }

        private string PathSetToString(HashSet<Resource> resourceSet)
        {
            return string.Join(", ",
                resourceSet.OrderBy(a => a.Name));
        }

        private string CallingSetToString(HashSet<string> callingSet)
        {
            return string.Join(", ", callingSet.OrderBy(a => a));
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

        [SmartContractReference("ContractC", "0x456745674567456745674567456745674567")]
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

        [SmartContractReference("_contractB", "0x123412341234123412341234123412341234")]
        private TestContractB _contractB;

        [SmartContractReference("ContractC", "0x456745674567456745674567456745674567")]
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
        [SmartContractReference("_contractB", "0x321132113211321132113211321132113211")]
        private TestContractB _contractB;

        [SmartContractReference("_contractB", "0x321132113211321132113211321132113211")]
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
        [SmartContractReference("_contractB", "0x123412341234123412341234123412341234")]
        private TestContractB _contractB;

        //want to call foreign function that Not Exist
        [SmartContractFunction("${this}.Func0", new string[] {"${_contractB}.Func10"}, new string[] { })]
        public void Func0()
        {
        }
    }

    #endregion
}