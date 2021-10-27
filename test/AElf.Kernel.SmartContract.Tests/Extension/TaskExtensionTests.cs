using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AElf.Kernel.SmartContract.Extension
{
    public class TaskExtensionTests
    {
        [Fact]
        public async Task Task_Extensions_Test_One_Cancellation()
        {
            var ct = new CancellationTokenSource(100);
            int times = 2;
            var task = CountWithReturnAsync(times, 100);
            await Assert.ThrowsAsync<OperationCanceledException>(() => task.WithCancellation(ct.Token));
        }

        [Fact]
        public async Task Task_Extensions_Test_One_Cancellation_NotCancelled()
        {
            var ct = new CancellationTokenSource(1000);
            int times = 2;
            var task = CountWithReturnAsync(times, 100);
            int res = await task.WithCancellation(ct.Token);
            Assert.Equal(times, res);
        }

        [Fact]
        public async Task Task_Extensions_Test_Two_BothCanceled()
        {
            var ct1 = new CancellationTokenSource(100);
            var ct2 = new CancellationTokenSource(100);
            int times = 2;
            var task = CountWithReturnAsync(times, 100);
            await Assert.ThrowsAsync<OperationCanceledException>(() => task.WithCancellation(ct1.Token, ct2.Token));
        }

        [Fact]
        public async Task Task_Extensions_Test_Two_Cancellations()
        {
            var ct1 = new CancellationTokenSource(1000);
            var ct2 = new CancellationTokenSource(100);
            int times = 2;
            var task = CountWithReturnAsync(times, 100);
            await Assert.ThrowsAsync<OperationCanceledException>(() => task.WithCancellation(ct1.Token, ct2.Token));
        }

        [Fact]
        public async Task Task_Extensions_Test_Two_CancellationTask_Extensions_Test_Two_Cancellations_NotCanceleds_NotCanceled()
        {
            var ct1 = new CancellationTokenSource(10000);
            var ct2 = new CancellationTokenSource(10000);
            int times = 2;
            var task = CountWithReturnAsync(times, 1000);
            var res = await task.WithCancellation(ct1.Token, ct2.Token);
            Assert.Equal(times, res);
        }

        [Fact]
        public async Task Task_Extensions_Test_WithOneCancellation()
        {
            int times = 2;
            var ct = new CancellationTokenSource(100);
            var task = CountWithoutReturnAsync(times, 100);
            await Assert.ThrowsAsync<OperationCanceledException>(() => task.WithCancellation(ct.Token));

            Counter = 0;
            var newCt = new CancellationTokenSource(1000);
            var newTask = CountWithoutReturnAsync(times, 100);
            await newTask.WithCancellation(newCt.Token);
            Assert.Equal(times, Counter);
        }

        [Fact]
        public async Task Task_Extensions_Test_WithTwoCancellation()
        {
            int times = 2;
            var ct1 = new CancellationTokenSource(100);
            var ct2 = new CancellationTokenSource(100);
            var task = CountWithoutReturnAsync(times, 100);
            await Assert.ThrowsAsync<OperationCanceledException>(() => task.WithCancellation(ct1.Token, ct2.Token));

            ct1 = new CancellationTokenSource(1000);
            ct2 = new CancellationTokenSource(100);
            task = CountWithoutReturnAsync(times, 100);
            await Assert.ThrowsAsync<OperationCanceledException>(() => task.WithCancellation(ct1.Token, ct2.Token));

            Counter = 0;
            ct1 = new CancellationTokenSource(1000);
            ct2 = new CancellationTokenSource(1000);
            task = CountWithoutReturnAsync(times, 100);
            await task.WithCancellation(ct1.Token, ct2.Token);
            Assert.Equal(times, Counter);
        }

        private async Task<int> CountWithReturnAsync(int times, int waitingPeriodMilliSecond)
        {
            int i = 0;
            while (i < times)
            {
                await Task.Delay(waitingPeriodMilliSecond);
                i++;
            }

            return i;
        }

        private static int Counter { get; set; }

        private async Task CountWithoutReturnAsync(int times, int waitingPeriodMilliSecond)
        {
            Counter = await CountWithReturnAsync(times, waitingPeriodMilliSecond);
        }
    }
}