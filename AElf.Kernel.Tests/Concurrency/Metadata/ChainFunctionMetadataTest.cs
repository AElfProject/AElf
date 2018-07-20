using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.SmartContract;
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
        private IDataStore _templateStore;
        

        public ChainFunctionMetadataTest(IDataStore templateStore)
        {
            _templateStore = templateStore ?? throw new ArgumentNullException(nameof(templateStore));
        }
        
        /*

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

            cfms.DeployNewContract("AElf.Kernel.Tests.Concurrency.Metadata.TestContractC", addrC, referenceBookForC);
            
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
            
            Assert.Equal(groundTruthMap, cfms.FunctionMetadataMap);

            
            referenceBookForB.Add("ContractC", addrC);
            cfms.DeployNewContract("AElf.Kernel.Tests.Concurrency.Metadata.TestContractB", addrB, referenceBookForB);
            
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
            
            Assert.Equal(groundTruthMap, cfms.FunctionMetadataMap);

            
            referenceBookForA.Add("ContractC", addrC);
            referenceBookForA.Add("_contractB", addrB);
            cfms.DeployNewContract("AElf.Kernel.Tests.Concurrency.Metadata.TestContractA", addrA, referenceBookForA);
            
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
            
            Assert.Equal(groundTruthMap, cfms.FunctionMetadataMap);

            //test restore
            ChainFunctionMetadataTemplate retoredTemplate  = new ChainFunctionMetadataTemplate(_templateStore, chainId, null);
            ChainFunctionMetadata newCFMS = new ChainFunctionMetadata(retoredTemplate, _templateStore, null);
            Assert.Equal(cfms.FunctionMetadataMap, newCFMS.FunctionMetadataMap);
            
            return cfms;
        }
        */
    }
}