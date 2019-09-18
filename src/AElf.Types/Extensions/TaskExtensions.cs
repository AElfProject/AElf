using System.Threading;
using System.Threading.Tasks;

namespace AElf
{
    public static class TaskExtensions
    {
        private struct Void { }
        public static async Task<TResult> WithCancellation<TResult>(this Task<TResult> originalTask, CancellationToken ct) 
        {
            var cancelTask = new TaskCompletionSource<Void>();
            using (ct.Register(t => ((TaskCompletionSource<Void>)t).TrySetResult(new Void()), cancelTask)) 
            {
                var any = await Task.WhenAny(originalTask, cancelTask.Task);
                if (any == cancelTask.Task) {
                    ct.ThrowIfCancellationRequested();                 
                }
            }
            return await originalTask;
        }
        public static async Task WithCancellation(this Task originalTask, CancellationToken ct)
        {
            var cancelTask = new TaskCompletionSource<Void>();
            using (ct.Register(t => ((TaskCompletionSource<Void>)t).TrySetResult(new Void()), cancelTask))
            {
                Task any = await Task.WhenAny(originalTask, cancelTask.Task);
                if (any == cancelTask.Task)
                {
                    ct.ThrowIfCancellationRequested();
                }
            }
            await originalTask;
        }
    }
}