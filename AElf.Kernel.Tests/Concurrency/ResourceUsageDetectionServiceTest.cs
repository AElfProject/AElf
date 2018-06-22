using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Concurrency.Scheduling;
using AElf.Kernel.Extensions;
using AElf.Kernel.Storages;
using AElf.Kernel.Tests.Concurrency.Metadata;
using Google.Protobuf;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests.Concurrency
{
    [UseAutofacTestFramework]
    public class ResourceUsageDetectionServiceTest
    {
        private IDataStore _dataStore;
        private IFunctionMetadataService _functionMetadataService;

        public ResourceUsageDetectionServiceTest(IDataStore dataStore, IFunctionMetadataService functionMetadataService)
        {
            _dataStore = dataStore;
            _functionMetadataService = functionMetadataService;
        }

        [Fact]
        public async Task TestResoruceDetection()
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
            
            ResourceUsageDetectionService detectionService = new ResourceUsageDetectionService(_functionMetadataService);

            var addr0 = new Hash("0".CalculateHash());
            var addr1 = new Hash("1".CalculateHash());
            
            var tx = new Transaction()
            {
                From = Hash.Zero,
                To = new Hash("TestContractA".CalculateHash()),
                IncrementId = 0,
                MethodName = "Func5",
                Params = ByteString.CopyFrom(new AElf.Kernel.Parameters()
                {
                    Params = {
                        new Param
                        {
                            HashVal = addr0
                        },
                        new Param
                        {
                            HashVal = addr1
                        },
                        new Param()
                        {
                            IntVal = 5
                        }
                    }
                }.ToByteArray())
            };

            var resources = detectionService.GetResources(chainId, tx);
            

            var groundTruth = new List<string>()
            {
                addrA.Value.ToBase64() + ".resource0" + "." + Hash.Zero.Value.ToBase64(),
                addrA.Value.ToBase64() + ".resource0" + "." + addr0.Value.ToBase64(),
                addrA.Value.ToBase64() + ".resource0" + "." + addr1.Value.ToBase64(),
                addrB.Value.ToBase64() + ".resource2" + "." + Hash.Zero.Value.ToBase64(),
                addrB.Value.ToBase64() + ".resource2" + "." + addr0.Value.ToBase64(),
                addrB.Value.ToBase64() + ".resource2" + "." + addr1.Value.ToBase64(),
                addrC.Value.ToBase64() + ".resource4" + "." + Hash.Zero.Value.ToBase64(),
                addrC.Value.ToBase64() + ".resource4" + "." + addr0.Value.ToBase64(),
                addrC.Value.ToBase64() + ".resource4" + "." + addr1.Value.ToBase64(),
                addrA.Value.ToBase64() + ".resource2"
            };

            Assert.Equal(groundTruth.OrderBy(a=>a).ToList(), resources.OrderBy(a=>a).ToList());
            
        }
    }
}