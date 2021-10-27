using System.Linq;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Types.Tests
{
    public class TransactionTraceTest
    {
        [Fact]
        public void TransactionTrace_Statistics_Test()
        {
            const int currentCount = 1;
            var currentTransactionTrace = GetTransactionTraceWithSameCount(currentCount);

            const int preTraceCount = 2;
            var preTransactionTrace = GetTransactionTraceWithSameCount(preTraceCount);
            const int prePreTraceCount = 1;
            var prePreTransactionTrace = GetTransactionTraceWithSameCount(prePreTraceCount);
            preTransactionTrace.PreTraces.Add(prePreTransactionTrace);
            currentTransactionTrace.PreTraces.Add(preTransactionTrace);
            
            const int inLineTraceCount = 2;
            var inLineTransactionTrace = GetTransactionTraceWithSameCount(inLineTraceCount);
            const int inlineInlinePreTraceCount = 1;
            var inlineInlineTransactionTrace = GetTransactionTraceWithSameCount(inlineInlinePreTraceCount);
            inLineTransactionTrace.InlineTraces.Add(inlineInlineTransactionTrace);
            currentTransactionTrace.InlineTraces.Add(inLineTransactionTrace);
            
            const int postTraceCount = 2;
            var postTransactionTrace = GetTransactionTraceWithSameCount(postTraceCount);
            const int postPostTraceCount = 1;
            var postPostTransactionTrace = GetTransactionTraceWithSameCount(postPostTraceCount);
            postPostTransactionTrace.PostTraces.Add(postTransactionTrace);
            currentTransactionTrace.PostTraces.Add(postPostTransactionTrace);
            currentTransactionTrace.FlattenedLogs.Count().ShouldBe(10);
            currentTransactionTrace.GetFlattenedReads().Count().ShouldBe(10);
            currentTransactionTrace.GetFlattenedWrites().Count().ShouldBe(10);
            currentTransactionTrace.GetStateSets().Count().ShouldBe(7);
        }

        private TransactionTrace GetTransactionTraceWithSameCount(int count)
        {
            return GetTransactionTrace(count, count, count, count);
        }

        private TransactionTrace GetTransactionTrace(int logEventCount = 0, int readsCount = 0, int writesCount = 0, int deletesCount = 0)
        {
            var transactionTrace = new TransactionTrace
            {
                StateSet = new TransactionExecutingStateSet()
            };
            var writeByteString = transactionTrace.ToByteString();
            transactionTrace.Logs.AddRange(Enumerable.Range(0, logEventCount).Select(x => new LogEvent()).ToArray());
            transactionTrace.StateSet.Reads.Add(Enumerable.Range(0, readsCount).ToDictionary(key=> key.ToString(), value => true));
            transactionTrace.StateSet.Writes.Add(Enumerable.Range(0, writesCount).ToDictionary(key=> key.ToString(), value => writeByteString));
            transactionTrace.StateSet.Deletes.Add(Enumerable.Range(0, deletesCount).ToDictionary(key=> key.ToString(), value => true));
            return transactionTrace;
        }
    }
}