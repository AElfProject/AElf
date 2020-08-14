using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Domain
{
    public class ExecutionReturnSetCollectionTests
    {
        [Fact]
        public void ExecutionReturnSetCollection_Test()
        {
            var executionReturnSetCollection = new ExecutionReturnSetCollection(new List<ExecutionReturnSet>());
            executionReturnSetCollection.GetExecutionReturnSetList().Count.ShouldBe(0);
            
            executionReturnSetCollection.AddRange(GetExecutionReturnSets());
            executionReturnSetCollection.Executed.Count.ShouldBe(2);
            executionReturnSetCollection.Executed.First(r => r.Status == TransactionResultStatus.Mined).TransactionId
                .ShouldBe(HashHelper.ComputeFrom("minedTxId"));
            executionReturnSetCollection.Executed.First(r => r.Status == TransactionResultStatus.Failed).TransactionId
                .ShouldBe(HashHelper.ComputeFrom("failedTxId"));
            var executionReturnSet = executionReturnSetCollection.Conflict.Single();
            executionReturnSet.Status.ShouldBe(TransactionResultStatus.Conflict);
            executionReturnSet.TransactionId.ShouldBe(HashHelper.ComputeFrom("conflictTxId"));

            var blockStateSet = executionReturnSetCollection.ToBlockStateSet();
            var change = blockStateSet.Changes.Single();
            change.Key.ShouldBe("secondKey");
            change.Value.ShouldBe(ByteString.Empty);
            blockStateSet.Deletes.Single().ShouldBe("firstKey");
        }

        private List<ExecutionReturnSet> GetExecutionReturnSets()
        {
            var firstKey = "firstKey";
            var secondKey = "secondKey";
            var executionReturnSets = new List<ExecutionReturnSet>();
            var minedTransactionId = HashHelper.ComputeFrom("minedTxId");
            var minedExecutionReturnSet = new ExecutionReturnSet
            {
                TransactionId = minedTransactionId,
                Status = TransactionResultStatus.Mined,
                StateChanges = {{"firstKey", ByteString.Empty}},
                StateDeletes = {{"secondKey", true}}
            };
            executionReturnSets.Add(minedExecutionReturnSet);
            
            var failedTransactionId =  HashHelper.ComputeFrom("failedTxId");
            var failedExecutionReturnSet = new ExecutionReturnSet
            {
                TransactionId = failedTransactionId,
                Status = TransactionResultStatus.Failed,
                StateChanges = {{"secondKey", ByteString.Empty}},
                StateDeletes = {{"firstKey", true}}
            };
            executionReturnSets.Add(failedExecutionReturnSet);
            
            var conflictTransactionId = HashHelper.ComputeFrom("conflictTxId");
            var conflictExecutionReturnSet = new ExecutionReturnSet
            {
                TransactionId = conflictTransactionId,
                Status = TransactionResultStatus.Conflict
            };
            executionReturnSets.Add(conflictExecutionReturnSet);
            return executionReturnSets;
        }
    }
}