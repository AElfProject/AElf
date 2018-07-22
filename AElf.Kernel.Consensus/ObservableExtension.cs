using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AElf.Kernel.Consensus
{
    public static class ObservableExtension
    {
        public static void Run(this IObservable<ConsensusBehavior> observable, IObserver<ConsensusBehavior> observer)
        {
            var resetEvent = new AutoResetEvent(false);

            observable
                .DoSet(() => resetEvent.Set())
                .Subscribe(observer);
            
            resetEvent.WaitOne();
        }

        public static void ConcatListThenRun(this IObservable<ConsensusBehavior> observable,
            List<IObservable<ConsensusBehavior>> others, IObserver<ConsensusBehavior> observer)
        {
            var resetEvent = new AutoResetEvent(false);

            foreach (var obs in others)
            {
                observable.Concat(obs);
            }

            observable
                .DoSet(() => resetEvent.Set())
                .Subscribe(observer);

            resetEvent.WaitOne();
        }


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