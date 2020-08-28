using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public class LogEventProcessingServiceTests : LogEventTestBase
    {
        private readonly ILogEventProcessingService<ITestLogEventProcessor> _logEventProcessingService;
        private readonly ITestLogEventProcessor _testLogEventProcessor;
        private readonly KernelTestHelper _kernelTestHelper;

        public LogEventProcessingServiceTests()
        {
            _logEventProcessingService = GetRequiredService<ILogEventProcessingService<ITestLogEventProcessor>>();
            _testLogEventProcessor = GetRequiredService<ITestLogEventProcessor>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task Process_Test()
        {
            var interestedLogEvent = new LogEvent
            {
                Name = "TestLogEvent",
                Address = SampleAddress.AddressList[0]
            };

            var notInterestedLogEvent = new LogEvent
            {
                Name = "TestLogEvent2",
                Address = SampleAddress.AddressList[0]
            };

            var notInterestedLogEvent2 = new LogEvent
            {
                Name = "TestLogEvent",
                Address = SampleAddress.AddressList[1]
            };

            var blockExecutedSetList = new List<BlockExecutedSet>();

            {
                blockExecutedSetList.Add(new BlockExecutedSet
                {
                    Block = _kernelTestHelper.GenerateBlock(9, HashHelper.ComputeFrom("PreviousBlockHash")),
                    TransactionResultMap = new Dictionary<Hash, TransactionResult>()
                });
                await _logEventProcessingService.ProcessAsync(blockExecutedSetList);

                var result = _testLogEventProcessor.GetProcessedResult();
                result.Count.ShouldBe(0);
            }

            {
                blockExecutedSetList.Add(GenerateBlockExecutedSet(10, new List<LogEvent>()));
                await _logEventProcessingService.ProcessAsync(blockExecutedSetList);

                var result = _testLogEventProcessor.GetProcessedResult();
                result.Count.ShouldBe(0);
            }

            {
                blockExecutedSetList.Add(GenerateBlockExecutedSet(11, new List<LogEvent>
                {
                    notInterestedLogEvent
                }));
                await _logEventProcessingService.ProcessAsync(blockExecutedSetList);

                var result = _testLogEventProcessor.GetProcessedResult();
                result.Count.ShouldBe(0);
            }

            {
                blockExecutedSetList.Add(GenerateBlockExecutedSet(12, new List<LogEvent>
                {
                    notInterestedLogEvent,
                    notInterestedLogEvent2
                }));
                await _logEventProcessingService.ProcessAsync(blockExecutedSetList);

                var result = _testLogEventProcessor.GetProcessedResult();
                result.Count.ShouldBe(0);
            }

            var blockExecutedSet13 = GenerateBlockExecutedSet(13, new List<LogEvent>
            {
                notInterestedLogEvent,
                notInterestedLogEvent2,
                interestedLogEvent
            });
            {
                blockExecutedSetList.Add(blockExecutedSet13);
                await _logEventProcessingService.ProcessAsync(blockExecutedSetList);

                var result = _testLogEventProcessor.GetProcessedResult();
                result.Count.ShouldBe(1);
                result[13].Values.Count.ShouldBe(1);
                result[13][blockExecutedSet13.TransactionResultMap[blockExecutedSet13.Block.Body.TransactionIds[3]]].Count
                    .ShouldBe(1);
                result[13][blockExecutedSet13.TransactionResultMap[blockExecutedSet13.Block.Body.TransactionIds[3]]][0]
                    .ShouldBe(interestedLogEvent);

                _testLogEventProcessor.CleanProcessedResult();
            }

            var blockExecutedSet14 = GenerateBlockExecutedSet(14, new List<LogEvent>
            {
                interestedLogEvent,
                interestedLogEvent
            });
            {
                blockExecutedSetList.Add(blockExecutedSet14);
                await _logEventProcessingService.ProcessAsync(blockExecutedSetList);

                var result = _testLogEventProcessor.GetProcessedResult();
                result.Count.ShouldBe(2);
                result[13].Values.Count.ShouldBe(1);
                result[13][blockExecutedSet13.TransactionResultMap[blockExecutedSet13.Block.Body.TransactionIds[3]]].Count
                    .ShouldBe(1);
                result[13][blockExecutedSet13.TransactionResultMap[blockExecutedSet13.Block.Body.TransactionIds[3]]][0]
                    .ShouldBe(interestedLogEvent);

                result[14].Values.Count.ShouldBe(2);
                result[14][blockExecutedSet14.TransactionResultMap[blockExecutedSet14.Block.Body.TransactionIds[1]]].Count
                    .ShouldBe(1);
                result[14][blockExecutedSet14.TransactionResultMap[blockExecutedSet14.Block.Body.TransactionIds[1]]][0]
                    .ShouldBe(interestedLogEvent);

                result[14][blockExecutedSet14.TransactionResultMap[blockExecutedSet14.Block.Body.TransactionIds[2]]].Count
                    .ShouldBe(2);
                result[14][blockExecutedSet14.TransactionResultMap[blockExecutedSet14.Block.Body.TransactionIds[2]]][0]
                    .ShouldBe(interestedLogEvent);
                result[14][blockExecutedSet14.TransactionResultMap[blockExecutedSet14.Block.Body.TransactionIds[2]]][1]
                    .ShouldBe(interestedLogEvent);

                _testLogEventProcessor.CleanProcessedResult();
            }
        }

        private BlockExecutedSet GenerateBlockExecutedSet(long blockHeight, List<LogEvent> logEvents)
        {
            var blockExecutedSet = new BlockExecutedSet();
            var transactions = new List<Transaction>();

            for (var i = 0; i < logEvents.Count + 1; i++)
            {
                transactions.Add(_kernelTestHelper.GenerateTransaction());
            }

            var block = _kernelTestHelper.GenerateBlock(blockHeight - 1, HashHelper.ComputeFrom("PreviousBlockHash"),
                transactions);
            blockExecutedSet.Block = block;
            blockExecutedSet.TransactionResultMap = new Dictionary<Hash, TransactionResult>
            {
                {
                    transactions[0].GetHash(),
                    new TransactionResult {TransactionId = transactions[0].GetHash(), Bloom = ByteString.Empty}
                }
            };

            var bloom = new Bloom();
            var logs = new List<LogEvent>();
            var txIndex = 1;
            foreach (var logEvent in logEvents)
            {
                bloom.Combine(new List<Bloom> {logEvent.GetBloom()});
                logs.Add(logEvent);

                var transactionResult = new TransactionResult
                {
                    TransactionId = transactions[txIndex].GetHash(),
                    Bloom = ByteString.CopyFrom(bloom.Data),
                };
                transactionResult.Logs.AddRange(logs);

                blockExecutedSet.TransactionResultMap.Add(transactions[txIndex].GetHash(), transactionResult);
                txIndex++;
            }

            block.Header.Bloom = ByteString.CopyFrom(bloom.Data);

            return blockExecutedSet;
        }
    }
}