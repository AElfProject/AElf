using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Extensions;
using NLog;
using ServiceStack;
using Xunit;
using Xunit.Abstractions;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.Concurrency.Scheduling
{
    public class FunctionMetadataServiceTest
    {
        
        [Fact]
        public void TestSetNewFunctionMetadata()
        {
            FunctionMetadataService functionMetadataService = new FunctionMetadataService(null);
            ParallelTestDataUtil util = new ParallelTestDataUtil();

            var pathSetForZ = new HashSet<Hash>();
            pathSetForZ.Add(new Hash("map1".CalculateHash()));
            Assert.True(functionMetadataService.SetNewFunctionMetadata("Z", new HashSet<string>(), pathSetForZ));
            Assert.Throws<InvalidOperationException>(() =>
                {
                    functionMetadataService.SetNewFunctionMetadata("Z", new HashSet<string>(), new HashSet<Hash>());
                });
            Assert.Equal(1, functionMetadataService.FunctionMetadataMap.Count);
            
            var faultCallingSet = new HashSet<string>();
            faultCallingSet.Add("Z");
            faultCallingSet.Add("U");
            Assert.False(functionMetadataService.SetNewFunctionMetadata("Y", faultCallingSet, new HashSet<Hash>()));
            Assert.Equal(1, functionMetadataService.FunctionMetadataMap.Count);
            
            var correctCallingSet = new HashSet<string>();
            correctCallingSet.Add("Z");
            var pathSetForY = new HashSet<Hash>();
            pathSetForY.Add(new Hash("list1".CalculateHash()));
            Assert.True(functionMetadataService.SetNewFunctionMetadata("Y", correctCallingSet, pathSetForY));
            
            Assert.Equal(2, functionMetadataService.FunctionMetadataMap.Count);
            Assert.Equal("[Y,(Z),(list1, map1)] [Z,(),(map1)]", util.FunctionMetadataMapToString(functionMetadataService.FunctionMetadataMap));
            return;
        }

        [Fact]
        public void TestNonTopologicalSetNewFunctionMetadata()
        {
            //Correct one
            
            ParallelTestDataUtil util = new ParallelTestDataUtil();
            var metadataList = util.GetFunctionMetadataMap(util.GetFunctionCallingGraph(), util.GetFunctionNonRecursivePathSet());
            FunctionMetadataService correctFunctionMetadataService = new FunctionMetadataService(null);
            foreach (var functionMetadata in metadataList)
            {
                Assert.True(correctFunctionMetadataService.SetNewFunctionMetadata(functionMetadata.Key,
                    functionMetadata.Value.CallingSet, functionMetadata.Value.NonRecursivePathSet));
            }
            
            Assert.Equal(util.FunctionMetadataMapToStringForTestData(metadataList.ToDictionary(a => a)), util.FunctionMetadataMapToString(correctFunctionMetadataService.FunctionMetadataMap));
            
            //Wrong one (there are circle where [P call O], [O call N], [N call P])
            metadataList.First(a => a.Key == "P").Value.CallingSet.Add("O");
            FunctionMetadataService wrongFunctionMetadataService = new FunctionMetadataService(null);
            foreach (var functionMetadata in metadataList)
            {
                if (!"PNO".Contains(functionMetadata.Key) )
                {
                    Assert.True(wrongFunctionMetadataService.SetNewFunctionMetadata(functionMetadata.Key,
                        functionMetadata.Value.CallingSet, functionMetadata.Value.NonRecursivePathSet));
                }
                else
                {
                    Assert.False(wrongFunctionMetadataService.SetNewFunctionMetadata(functionMetadata.Key,
                        functionMetadata.Value.CallingSet, functionMetadata.Value.NonRecursivePathSet));
                }
            }
        }
        
        //TODO: update functionalities test needed
    }
}