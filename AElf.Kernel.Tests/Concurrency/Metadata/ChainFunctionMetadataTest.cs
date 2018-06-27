using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Storages;
using AElf.Kernel.Tests.Concurrency.Scheduling;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using ServiceStack;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.Concurrency.Metadata
{
    [UseAutofacTestFramework]
    public class ChainFunctionMetadataTest
    {
    
        private ParallelTestDataUtil util = new ParallelTestDataUtil();
        
        private IDataStore _templateStore;
        

        public ChainFunctionMetadataTest(IDataStore templateStore)
        {
            _templateStore = templateStore ?? throw new ArgumentNullException(nameof(templateStore));
        }

        [Fact]
        public async Task<ChainFunctionMetadata> TestDeployNewFunction()
        {
            var template = new ChainFunctionMetadataTemplate(_templateStore, Hash.Zero, null);
            Hash chainId = template.ChainId;
            template.CallingGraph.Clear();
            template.ContractMetadataTemplateMap.Clear();
            await template.TryAddNewContract(typeof(TestContractC));
            await template.TryAddNewContract(typeof(TestContractB));
            await template.TryAddNewContract(typeof(TestContractA));
            ChainFunctionMetadata cfms = new ChainFunctionMetadata(template, _templateStore, null);
            
            cfms.FunctionMetadataMap.Clear();


            var addrA = new Hash("TestContractA".CalculateHash());
            var addrB = new Hash("TestContractB".CalculateHash());
            var addrC = new Hash("TestContractC".CalculateHash());
            
            var referenceBookForA = new Dictionary<string, Hash>();
            var referenceBookForB = new Dictionary<string, Hash>();
            var referenceBookForC = new Dictionary<string, Hash>();
            
            var groundTruthMap = new Dictionary<string, FunctionMetadata>();

            cfms.DeployNewContract("TestContractC", addrC, referenceBookForC);
            
            groundTruthMap.Add(
                addrC.Value.ToBase64() + ".Func0", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToBase64() + ".resource4", DataAccessMode.AccountSpecific)
                    }), 
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToBase64() + ".resource4", DataAccessMode.AccountSpecific)
                    })));
            
            groundTruthMap.Add(
                addrC.Value.ToBase64() + ".Func1", 
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToBase64() + ".resource5", DataAccessMode.ReadOnlyAccountSharing) 
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToBase64() + ".resource5", DataAccessMode.ReadOnlyAccountSharing) 
                    })));
            
            Assert.Equal(util.FunctionMetadataMapToString(groundTruthMap), util.FunctionMetadataMapToString(cfms.FunctionMetadataMap));

            
            referenceBookForB.Add("ContractC", addrC);
            cfms.DeployNewContract("TestContractB", addrB, referenceBookForB);
            
            groundTruthMap.Add(
                addrB.Value.ToBase64() + ".Func0",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrC.Value.ToBase64() + ".Func1"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrC.Value.ToBase64() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrB.Value.ToBase64() + ".resource2", DataAccessMode.AccountSpecific), 
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrB.Value.ToBase64() + ".resource2", DataAccessMode.AccountSpecific), 
                    })));
            
            groundTruthMap.Add(
                addrB.Value.ToBase64() + ".Func1",
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrB.Value.ToBase64() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrB.Value.ToBase64() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                    })));
            
            Assert.Equal(util.FunctionMetadataMapToString(groundTruthMap), util.FunctionMetadataMapToString(cfms.FunctionMetadataMap));

            
            referenceBookForA.Add("ContractC", addrC);
            referenceBookForA.Add("_contractB", addrB);
            cfms.DeployNewContract("TestContractA", addrA, referenceBookForA);
            
            groundTruthMap.Add(
                addrA.Value.ToBase64() + ".Func0(int)",
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(),
                    new HashSet<Resource>()));
            
            groundTruthMap.Add(
                addrA.Value.ToBase64() + ".Func0",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.Value.ToBase64() + ".Func1"
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrA.Value.ToBase64() + ".resource0", DataAccessMode.AccountSpecific),
                        new Resource(addrA.Value.ToBase64() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToBase64() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    }),
                    new HashSet<Resource>(new []
                    {
                        new Resource(addrA.Value.ToBase64() + ".resource0", DataAccessMode.AccountSpecific),
                    })));
            
            groundTruthMap.Add(
                addrA.Value.ToBase64() + ".Func1",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.Value.ToBase64() + ".Func2"
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToBase64() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToBase64() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToBase64() + ".resource1", DataAccessMode.ReadOnlyAccountSharing)
                    })));
            
            groundTruthMap.Add(
                addrA.Value.ToBase64() + ".Func2",
                new FunctionMetadata(
                    new HashSet<string>(),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToBase64() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToBase64() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToBase64() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToBase64() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    })));
            
            groundTruthMap.Add(
                addrA.Value.ToBase64() + ".Func3",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.Value.ToBase64() + ".Func0",
                        addrB.Value.ToBase64() + ".Func0", 
                        addrC.Value.ToBase64() + ".Func0"
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToBase64() + ".resource0", DataAccessMode.AccountSpecific),
                        new Resource(addrB.Value.ToBase64() + ".resource2", DataAccessMode.AccountSpecific), 
                        new Resource(addrC.Value.ToBase64() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrC.Value.ToBase64() + ".resource4", DataAccessMode.AccountSpecific),
                        new Resource(addrA.Value.ToBase64() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToBase64() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToBase64() + ".resource1", DataAccessMode.ReadOnlyAccountSharing)
                    })));
            
            groundTruthMap.Add(
                addrA.Value.ToBase64() + ".Func4",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.Value.ToBase64() + ".Func2"
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToBase64() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToBase64() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    }),
                    new HashSet<Resource>()));
            
            groundTruthMap.Add(
                addrA.Value.ToBase64() + ".Func5",
                new FunctionMetadata(
                    new HashSet<string>(new []
                    {
                        addrA.Value.ToBase64() + ".Func3",
                        addrB.Value.ToBase64() + ".Func1"
                    }),
                    new HashSet<Resource>(new[]
                    {
                        new Resource(addrA.Value.ToBase64() + ".resource0", DataAccessMode.AccountSpecific),
                        new Resource(addrB.Value.ToBase64() + ".resource3", DataAccessMode.ReadOnlyAccountSharing), 
                        new Resource(addrB.Value.ToBase64() + ".resource2", DataAccessMode.AccountSpecific), 
                        new Resource(addrC.Value.ToBase64() + ".resource5", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrC.Value.ToBase64() + ".resource4", DataAccessMode.AccountSpecific),
                        new Resource(addrA.Value.ToBase64() + ".resource1", DataAccessMode.ReadOnlyAccountSharing),
                        new Resource(addrA.Value.ToBase64() + ".resource2", DataAccessMode.ReadWriteAccountSharing)
                    }),
                    new HashSet<Resource>()));
            
            Assert.Equal(util.FunctionMetadataMapToString(groundTruthMap), util.FunctionMetadataMapToString(cfms.FunctionMetadataMap));

            //test restore
            ChainFunctionMetadataTemplate retoredTemplate  = new ChainFunctionMetadataTemplate(_templateStore, chainId, null);
            ChainFunctionMetadata newCFMS = new ChainFunctionMetadata(retoredTemplate, _templateStore, null);
            Assert.Equal(util.FunctionMetadataMapToString(cfms.FunctionMetadataMap), util.FunctionMetadataMapToString(newCFMS.FunctionMetadataMap));
            
            return cfms;
        }

        
        [Fact]
        public void TestSetNewFunctionMetadata()
        {    /*
            ChainFunctionMetadata functionMetadataService = new ChainFunctionMetadata(null);
            ParallelTestDataUtil util = new ParallelTestDataUtil();

            var pathSetForZ = new HashSet<string>();
            pathSetForZ.Add("map1");
            Assert.True(functionMetadataService.DeployNewFunction("Z", new HashSet<string>(), pathSetForZ));
            Assert.Throws<InvalidOperationException>(() =>
                {
                    functionMetadataService.DeployNewFunction("Z", new HashSet<string>(), new HashSet<string>());
                });
            Assert.Equal(1, functionMetadataService.FunctionMetadataMap.Count);
            
            var faultCallingSet = new HashSet<string>();
            faultCallingSet.Add("Z");
            faultCallingSet.Add("U");
            Assert.False(functionMetadataService.DeployNewFunction("Y", faultCallingSet, new HashSet<string>()));
            Assert.Equal(1, functionMetadataService.FunctionMetadataMap.Count);
            
            var correctCallingSet = new HashSet<string>();
            correctCallingSet.Add("Z");
            var pathSetForY = new HashSet<string>();
            pathSetForY.Add("list1");
            Assert.True(functionMetadataService.DeployNewFunction("Y", correctCallingSet, pathSetForY));
            
            Assert.Equal(2, functionMetadataService.FunctionMetadataMap.Count);
            Assert.Equal("[Y,(Z),(list1, map1)] [Z,(),(map1)]", util.FunctionMetadataMapToString(functionMetadataService.FunctionMetadataMap));
            return;
            */
        }

        [Fact]
        public void TestNonTopologicalSetNewFunctionMetadata()
        {
            /*
            //correct one
            ParallelTestDataUtil util = new ParallelTestDataUtil();
            var metadataList = util.GetFunctionMetadataMap(util.GetFunctionCallingGraph(), util.GetFunctionNonRecursivePathSet());
            ChainFunctionMetadata correctFunctionMetadataService = new ChainFunctionMetadata(null);
            foreach (var functionMetadata in metadataList)
            {
                Assert.True(correctFunctionMetadataService.DeployNewFunction(functionMetadata.Key,
                    functionMetadata.Value.CallingSet, functionMetadata.Value.LocalResourceSet));
            }
            
            Assert.Equal(util.FunctionMetadataMapToStringForTestData(metadataList.ToDictionary(a => a)), util.FunctionMetadataMapToString(correctFunctionMetadataService.FunctionMetadataMap));
            
            //Wrong one (there are circle where [P call O], [O call N], [N call P])
            metadataList.First(a => a.Key == "P").Value.CallingSet.Add("O");
            ChainFunctionMetadata wrongFunctionMetadataService = new ChainFunctionMetadata(null);
            foreach (var functionMetadata in metadataList)
            {
                if (!"PNO".Contains(functionMetadata.Key) )
                {
                    Assert.True(wrongFunctionMetadataService.DeployNewFunction(functionMetadata.Key,
                        functionMetadata.Value.CallingSet, functionMetadata.Value.LocalResourceSet));
                }
                else
                {
                    Assert.False(wrongFunctionMetadataService.DeployNewFunction(functionMetadata.Key,
                        functionMetadata.Value.CallingSet, functionMetadata.Value.LocalResourceSet));
                }
            }
            */
        }
        
        //TODO: update functionalities test needed
    }
}