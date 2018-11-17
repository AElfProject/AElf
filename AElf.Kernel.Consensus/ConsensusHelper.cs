using System;
using System.Reactive.Linq;
using AElf.Common;

namespace AElf.Kernel.Consensus
{
    public class ConsensusHelper
    {
        // ReSharper disable once MemberCanBeMadeStatic.Local
        /// <summary>
        /// Get interval observable.
        /// </summary>
        /// <param name="interval">Milliseconds</param>
        /// <returns></returns>
        public IObservable<long> GetIntervalObservable(int interval = GlobalConfig.AElfLogInterval)
        {
            return Observable.Interval(TimeSpan.FromMilliseconds(interval));
        }
    }
}