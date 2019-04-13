using System;
using System.Linq;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Types.Tests
{
    public class TransactionParallelMetaDataTests
    {
        private TransactionParallelMetaData _parallelMetaData;
        public TransactionParallelMetaDataTests()
        {
            _parallelMetaData = new TransactionParallelMetaData();
        }
        
        [Fact]
        public void Transaction_ParallelMetaData_Test()
        {
            Should.Throw<NotImplementedException>(()=>_parallelMetaData.IsParallel());

            var dataList = _parallelMetaData.GetDataConflict();
            
            dataList.ShouldNotBeNull();
            dataList.Count().ShouldBe(2);
            dataList.ShouldAllBe(data => data == null);
        }
    }
}