using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AElf.Kernel.Consensus
{
    public static class ObservableExtension
    {
        public static IObservable<T> DoSet<T>(this IObservable<T> observable, Action action, TimeSpan? delay = null)
        {
            async void DelayedAction()
            {
                if (delay.HasValue)
                {
                    await Task.Delay(delay.Value);
                }

                action();
            }

            return observable.Do(
                _ => { },
                _ => DelayedAction(),
                DelayedAction);
        }
    }
}