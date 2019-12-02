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
            var task = CountAsync(times, 100);
            await Assert.ThrowsAsync<OperationCanceledException>(() => task.WithCancellation(ct.Token));
        }

        [Fact]
        public async Task Task_Extensions_Test_One_Cancellation_NotCancelled()
        {
            var ct = new CancellationTokenSource(300);
            int times = 2;
            var task = CountAsync(times, 100);
            int res = await task.WithCancellation(ct.Token);
            Assert.Equal(times, res);
        }

        [Fact]
        public async Task Task_Extensions_Test_Two_BothCanceled()
        {
            var ct1 = new CancellationTokenSource(100);
            var ct2 = new CancellationTokenSource(100);
            int times = 2;
            var task = CountAsync(times, 100);
            await Assert.ThrowsAsync<OperationCanceledException>(() => task.WithCancellation(ct1.Token, ct2.Token));
        }

        [Fact]
        public async Task Task_Extensions_Test_Two_Cancellations()
        {
            var ct1 = new CancellationTokenSource(300);
            var ct2 = new CancellationTokenSource(100);
            int times = 2;
            var task = CountAsync(times, 100);
            await Assert.ThrowsAsync<OperationCanceledException>(() => task.WithCancellation(ct1.Token, ct2.Token));
        }

        [Fact]
        public async Task Task_Extensions_Test_Two_Cancellations_NotCanceled()
        {
            var ct1 = new CancellationTokenSource(3000);
            var ct2 = new CancellationTokenSource(3000);
            int times = 2;
            var task = CountAsync(times, 1000);
            var res = await task.WithCancellation(ct1.Token, ct2.Token);
            Assert.Equal(times, res);
        }

        private async Task<int> CountAsync(int times, int waitingPeriodMilliSecond)
        {
            int i = 0;
            while (i < times)
            {
                await Task.Delay(waitingPeriodMilliSecond);
                i++;
            }

            return i;
        }
    }
}