using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Tests.Concurrency.Scheduling;
using ServiceStack;
using Xunit;

namespace AElf.Kernel.Tests.Concurrency.Metadata
{
    public class ChainFunctionMetadataServiceTest
    {
        
        [Fact]
        public void TestSetNewFunctionMetadata()
        {    /*
            ChainFunctionMetadataService functionMetadataService = new ChainFunctionMetadataService(null);
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
            ChainFunctionMetadataService correctFunctionMetadataService = new ChainFunctionMetadataService(null);
            foreach (var functionMetadata in metadataList)
            {
                Assert.True(correctFunctionMetadataService.DeployNewFunction(functionMetadata.Key,
                    functionMetadata.Value.CallingSet, functionMetadata.Value.LocalResourceSet));
            }
            
            Assert.Equal(util.FunctionMetadataMapToStringForTestData(metadataList.ToDictionary(a => a)), util.FunctionMetadataMapToString(correctFunctionMetadataService.FunctionMetadataMap));
            
            //Wrong one (there are circle where [P call O], [O call N], [N call P])
            metadataList.First(a => a.Key == "P").Value.CallingSet.Add("O");
            ChainFunctionMetadataService wrongFunctionMetadataService = new ChainFunctionMetadataService(null);
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