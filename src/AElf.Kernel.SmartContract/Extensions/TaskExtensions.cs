using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract
{
    //TODO: Move TaskExtensions to lower level. 
    public static class TaskExtensions
    {
        private struct Void
        {
        }
        
        public static async Task<TResult> WithCancellation<TResult>(this Task<TResult> originalTask, CancellationToken ct) 
        {
            var cancelTask = new TaskCompletionSource<Void>();
            using (ct.Register(SetTaskResultCallback, cancelTask))
            {
                await RunTaskWithCompletionSource(originalTask, new Dictionary<CancellationToken, Task>
                {
                    {ct, cancelTask.Task}
                });
            }
            
            return await originalTask;
        }
        
        public static async Task<TResult> WithCancellation<TResult>(this Task<TResult> originalTask, CancellationToken ct1, CancellationToken ct2)
        {
            var cancelTask1 = new TaskCompletionSource<Void>();
            var cancelTask2 = new TaskCompletionSource<Void>();
            using (ct1.Register(SetTaskResultCallback, cancelTask1))
            using (ct2.Register(SetTaskResultCallback, cancelTask2))
            {
                await RunTaskWithCompletionSource(originalTask, new Dictionary<CancellationToken, Task>
                {
                    {ct1, cancelTask1.Task},
                    {ct2, cancelTask2.Task}
                });
            }

            return await originalTask;
        }
        
        public static async Task WithCancellation(this Task originalTask, CancellationToken ct)
        {
            var cancelTask = new TaskCompletionSource<Void>();
            using (ct.Register(SetTaskResultCallback, cancelTask))
            {
                await RunTaskWithCompletionSource(originalTask, new Dictionary<CancellationToken, Task>
                {
                    {ct, cancelTask.Task}
                });
            }

            await originalTask;
        }
        
        
        public static async Task WithCancellation(this Task originalTask, CancellationToken ct1, CancellationToken ct2)
        {
            var cancelTask1 = new TaskCompletionSource<Void>();
            var cancelTask2 = new TaskCompletionSource<Void>();
            using (ct1.Register(SetTaskResultCallback, cancelTask1))
            using (ct2.Register(SetTaskResultCallback, cancelTask2))
            {
                await RunTaskWithCompletionSource(originalTask, new Dictionary<CancellationToken, Task>
                {
                    {ct1, cancelTask1.Task},
                    {ct2, cancelTask2.Task}
                });
            }

            await originalTask;
        }

        private static void SetTaskResultCallback(object obj)
        {
            ((TaskCompletionSource<Void>) obj).TrySetResult(new Void());
        }

        private static async Task RunTaskWithCompletionSource(Task originalTask, 
            Dictionary<CancellationToken, Task> completionSourceTokenTaskDict)
        {
            var any = await Task.WhenAny(completionSourceTokenTaskDict.Values.ToList().Append(originalTask));

            foreach (var completionSourceTokenTask in completionSourceTokenTaskDict)
            {
                if (any == completionSourceTokenTask.Value)
                {
                    completionSourceTokenTask.Key.ThrowIfCancellationRequested();
                }
            }
        }
    }
}