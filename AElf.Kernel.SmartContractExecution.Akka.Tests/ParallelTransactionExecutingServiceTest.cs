//using System;
//using System.Linq;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Xunit;
//using Akka.Actor;
//using Akka.TestKit;
//using Akka.TestKit.Xunit;
//using AElf.Kernel.SmartContractExecution;
//using AElf.Kernel.SmartContractExecution.Scheduling;
//using AElf.Kernel.Core.Tests.Concurrency.Execution;
//using AElf.SmartContract;
//using AElf.Types.CSharp;
//using Google.Protobuf;
//using Google.Protobuf.WellKnownTypes;
//using Type = System.Type;
//using AElf.Common;
//using Microsoft.Extensions.Options;
//using Address = AElf.Common.Address;

namespace AElf.Kernel.Tests.Concurrency
{
    /*
    public class ParallelTransactionExecutingServiceTest : AElfAkkaTestKitBase
    {
        private MockSetup _mock;
        private readonly IOptionsSnapshot<ExecutionOptions> _executionOptions;

        public ParallelTransactionExecutingServiceTest() : base(new XunitAssertions())
        {
            _mock = this._aelfKernelIntegratedTest.GetRequiredService<MockSetup>();
            _executionOptions = _aelfKernelIntegratedTest.GetRequiredService<IOptionsSnapshot<ExecutionOptions>>();
        }

        [Fact]
        public async Task TwoJobsTest()
        {
            var balances = new List<int>()
            {
                100,
                0
            };
            var addresses = Enumerable.Range(0, balances.Count).Select(x => Address.Generate()).ToList();

            foreach (var addbal in addresses.Zip(balances, Tuple.Create))
            {
                _mock.Initialize1(addbal.Item1, (ulong) addbal.Item2);
            }

            var txs = new List<Transaction>()
            {
                _mock.GetTransferTxn1(addresses[0], addresses[1], 10),
            };
            var txsHashes = txs.Select(y => y.GetHash()).ToList();

            var finalBalances = new List<int>
            {
                90,
                10
            };

            var service = new NoFeeParallelTransactionExecutingService(_mock.ActorEnvironment,
                new Grouper(_mock.ServicePack.ResourceDetectionService),_mock.ServicePack, _executionOptions);

            var traces = await service.ExecuteAsync(txs, _mock.ChainId1, DateTime.UtcNow, CancellationToken.None);

            foreach (var txTrace in txs.Zip(traces, Tuple.Create))
            {
                Assert.Equal(txTrace.Item1.GetHash(), txTrace.Item2.TransactionId);
                if (!string.IsNullOrEmpty(txTrace.Item2.StdErr))
                {
                    Assert.Null(txTrace.Item2.StdErr);
                }
            }

            foreach (var addFinbal in addresses.Zip(finalBalances, Tuple.Create))
            {
                Assert.Equal((ulong) addFinbal.Item2, _mock.GetBalance1(addFinbal.Item1));
            }
        }

        [Fact]
        public async Task ManyJobsTest()
        {
//             *  Job 1: (0-1, 10), (1-2, 9)
//             *  Job 2: (3-4, 8)
//             *  Job 3: (5-6, 10)

            var balances = new List<ulong>()
            {
                100,
                0,
                0,
                200,
                0,
                300,
                0
            };
            var addresses = Enumerable.Range(0, balances.Count).Select(x => Address.Generate()).ToList();

            foreach (var addbal in addresses.Zip(balances, Tuple.Create))
            {
                _mock.Initialize1(addbal.Item1, addbal.Item2);
            }

            var txs = new List<Transaction>()
            {
                _mock.GetTransferTxn1(addresses[0], addresses[1], 10),
                _mock.GetTransferTxn1(addresses[1], addresses[2], 9),
                _mock.GetTransferTxn1(addresses[3], addresses[4], 8),
                _mock.GetTransferTxn1(addresses[5], addresses[6], 10),
            };

            var finalBalances = new List<ulong>
            {
                90,
                1,
                9,
                192,
                8,
                290,
                10
            };

            var service = new NoFeeParallelTransactionExecutingService(_mock.ActorEnvironment,
                new Grouper(_mock.ServicePack.ResourceDetectionService),_mock.ServicePack, _executionOptions);

            var traces = await service.ExecuteAsync(txs, _mock.ChainId1, DateTime.UtcNow, CancellationToken.None);

            foreach (var txTrace in txs.Zip(traces, Tuple.Create))
            {
                Assert.Equal(txTrace.Item1.GetHash(), txTrace.Item2.TransactionId);
                if (!string.IsNullOrEmpty(txTrace.Item2.StdErr))
                {
                    Assert.Null(txTrace.Item2.StdErr);
                }
            }

            Assert.Equal(
                string.Join(" ", finalBalances),
                string.Join(" ", addresses.Select(a => _mock.GetBalance1(a)))
            );
        }

        [Fact]
        public async Task InlineTransactionTest()
        {
            int rep1 = 4;
            int rep2 = 5;
            var txs = new List<Transaction>()
            {
                new Transaction()
                {
                    From = Address.Zero,
                    To = _mock.SampleContractAddress1,
                    MethodName = "InlineTxnBackToSelf",
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(rep1))
                },
                new Transaction()
                {
                    From = Address.Zero,
                    To = _mock.SampleContractAddress1,
                    MethodName = "InlineTxnBackToSelf",
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(rep2))
                }
            };
            Func<TransactionTrace, TransactionTrace> getInnerMostTrace = null;
            getInnerMostTrace = (tr) =>
            {
                if (tr.InlineTraces.Count == 0)
                {
                    return tr;
                }
                return getInnerMostTrace(tr.InlineTraces.First());
            };
            var service = new NoFeeParallelTransactionExecutingService(_mock.ActorEnvironment,
                new Grouper(_mock.ServicePack.ResourceDetectionService),_mock.ServicePack, _executionOptions);
            var traces = await service.ExecuteAsync(txs, _mock.ChainId1, DateTime.UtcNow, CancellationToken.None);
            Assert.NotEqual(ExecutionStatus.ExceededMaxCallDepth, getInnerMostTrace(traces[0]).ExecutionStatus);
            Assert.Equal(ExecutionStatus.ExceededMaxCallDepth, getInnerMostTrace(traces[1]).ExecutionStatus);
        }
    }
    */
}