using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Frameworks.Autofac;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Concurrency.Scheduling;
using AElf.Kernel.Tests.Concurrency.Execution;

namespace AElf.Kernel.Tests.Concurrency
{
    [UseAutofacTestFramework]
    public class ParallelTransactionExecutingServiceTest : TestKitBase
    {
        private MockSetup _mock;

        public ParallelTransactionExecutingServiceTest(MockSetup mock) : base(new XunitAssertions())
        {
            _mock = mock;
        }

        [Fact]
        public async Task TwoJobsTest()
        {
            var balances = new List<int>()
            {
                100,
                0
            };
            var addresses = Enumerable.Range(0, balances.Count).Select(x => Hash.Generate()).ToList();

            foreach (var addbal in addresses.Zip(balances, Tuple.Create))
            {
                _mock.Initialize1(addbal.Item1, (ulong) addbal.Item2);
            }

            var txs = new List<ITransaction>()
            {
                _mock.GetTransferTxn1(addresses[0], addresses[1], 10),
            };
            var txsHashes = txs.Select(y => y.GetHash()).ToList();

            var finalBalances = new List<int>
            {
                90,
                10
            };

            var service = new ParallelTransactionExecutingService(_mock.Requestor,
                new Grouper(_mock.ServicePack.ResourceDetectionService));

            var traces = await service.ExecuteAsync(txs, _mock.ChainId1);

            foreach (var txTrace in txs.Zip(traces, Tuple.Create))
            {
                Assert.Equal(txTrace.Item1.GetHash(), txTrace.Item2.TransactionId);
                Assert.True(string.IsNullOrEmpty(txTrace.Item2.StdErr));
            }

            foreach (var addFinbal in addresses.Zip(finalBalances, Tuple.Create))
            {
                Assert.Equal((ulong) addFinbal.Item2, _mock.GetBalance1(addFinbal.Item1));
            }
        }

        [Fact]
        public async Task ManyJobsTest()
        {
            /*
             *  Job 1: (0-1, 10), (1-2, 9)
             *  Job 2: (3-4, 8)
             *  Job 3: (5-6, 10)
             */

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
            var addresses = Enumerable.Range(0, balances.Count).Select(x => Hash.Generate()).ToList();

            foreach (var addbal in addresses.Zip(balances, Tuple.Create))
            {
                _mock.Initialize1(addbal.Item1, addbal.Item2);
            }

            var txs = new List<ITransaction>()
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

            var service = new ParallelTransactionExecutingService(_mock.Requestor,
                new Grouper(_mock.ServicePack.ResourceDetectionService));

            var traces = await service.ExecuteAsync(txs, _mock.ChainId1);

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
    }
}