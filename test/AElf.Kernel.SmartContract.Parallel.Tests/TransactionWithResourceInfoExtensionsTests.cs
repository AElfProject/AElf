using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Parallel.Domain;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    public class TransactionWithResourceInfoExtensionsTests
    {
        [Fact]
        public void TransactionWithResourceInfoExtensionsTest()
        {
            var transactionWithResourceInfos = new List<TransactionWithResourceInfo>()
            {
                new TransactionWithResourceInfo
                {
                    TransactionResourceInfo = new TransactionResourceInfo
                    {
                        ReadPaths = {new[] {1, 2, 3}.Select(GetPath)},
                        WritePaths = {new[] {2, 3, 4}.Select(GetPath)}
                    }
                }
            };
            var readOnlyPaths = transactionWithResourceInfos.GetReadOnlyPaths();
            readOnlyPaths.Count.ShouldBe(1);
            readOnlyPaths.First().ShouldBe(GetPath(1));
        }
        
        private ScopedStatePath GetPath(int value)
        {
            return new ScopedStatePath
            {
                Address = SampleAddress.AddressList[0],
                Path = new StatePath
                {
                    Parts = {value.ToString()}
                }
            };
        }
    }
}