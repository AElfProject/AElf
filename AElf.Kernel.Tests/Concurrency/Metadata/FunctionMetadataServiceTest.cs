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
    public class ChainFunctionMetadataTemplateServiceTest
    {
        
        [Fact]
        public void TestSetNewFunctionMetadata()
        {
            ChainFunctionMetadataTemplateService functionMetadataService = new ChainFunctionMetadataTemplateService(null);
            ParallelTestDataUtil util = new ParallelTestDataUtil();

            var pathSetForZ = new HashSet<string>();
            pathSetForZ.Add("map1");
            Assert.True(functionMetadataService.SetNewFunctionMetadata("Z", new HashSet<string>(), pathSetForZ));
            Assert.Throws<InvalidOperationException>(() =>
                {
                    functionMetadataService.SetNewFunctionMetadata("Z", new HashSet<string>(), new HashSet<string>());
                });
            Assert.Equal(1, functionMetadataService.FunctionMetadataTemplateMap.Count);
            
            var faultCallingSet = new HashSet<string>();
            faultCallingSet.Add("Z");
            faultCallingSet.Add("U");
            Assert.False(functionMetadataService.SetNewFunctionMetadata("Y", faultCallingSet, new HashSet<string>()));
            Assert.Equal(1, functionMetadataService.FunctionMetadataTemplateMap.Count);
            
            var correctCallingSet = new HashSet<string>();
            correctCallingSet.Add("Z");
            var pathSetForY = new HashSet<string>();
            pathSetForY.Add("list1");
            Assert.True(functionMetadataService.SetNewFunctionMetadata("Y", correctCallingSet, pathSetForY));
            
            Assert.Equal(2, functionMetadataService.FunctionMetadataTemplateMap.Count);
            Assert.Equal("[Y,(Z),(list1, map1)] [Z,(),(map1)]", util.FunctionMetadataMapToString(functionMetadataService.FunctionMetadataTemplateMap));
            return;
        }

        [Fact]
        public void TestNonTopologicalSetNewFunctionMetadata()
        {
            //Correct one
            
            ParallelTestDataUtil util = new ParallelTestDataUtil();
            var metadataList = util.GetFunctionMetadataMap(util.GetFunctionCallingGraph(), util.GetFunctionNonRecursivePathSet());
            ChainFunctionMetadataTemplateService correctFunctionMetadataService = new ChainFunctionMetadataTemplateService(null);
            foreach (var functionMetadata in metadataList)
            {
                Assert.True(correctFunctionMetadataService.SetNewFunctionMetadata(functionMetadata.Key,
                    functionMetadata.Value.CallingSet, functionMetadata.Value.LocalResourceSet));
            }
            
            Assert.Equal(util.FunctionMetadataMapToStringForTestData(metadataList.ToDictionary(a => a)), util.FunctionMetadataMapToString(correctFunctionMetadataService.FunctionMetadataTemplateMap));
            
            //Wrong one (there are circle where [P call O], [O call N], [N call P])
            metadataList.First(a => a.Key == "P").Value.CallingSet.Add("O");
            ChainFunctionMetadataTemplateService wrongFunctionMetadataService = new ChainFunctionMetadataTemplateService(null);
            foreach (var functionMetadata in metadataList)
            {
                if (!"PNO".Contains(functionMetadata.Key) )
                {
                    Assert.True(wrongFunctionMetadataService.SetNewFunctionMetadata(functionMetadata.Key,
                        functionMetadata.Value.CallingSet, functionMetadata.Value.LocalResourceSet));
                }
                else
                {
                    Assert.False(wrongFunctionMetadataService.SetNewFunctionMetadata(functionMetadata.Key,
                        functionMetadata.Value.CallingSet, functionMetadata.Value.LocalResourceSet));
                }
            }
        }
        
        //TODO: update functionalities test needed
    }
}